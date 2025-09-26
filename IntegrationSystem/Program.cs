using System;
using System.Linq;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using IntegrationSystem.Data;
using IntegrationSystem.Models;
using EasyModbus;

const short AuthKey = unchecked((short)0xBEEF);

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

var conn = config.GetConnectionString("Default")
           ?? Environment.GetEnvironmentVariable("ConnectionStrings__Default")
           ?? throw new InvalidOperationException("Missing connection string 'Default'.");

var dbOptions = new DbContextOptionsBuilder<BridgeDbContext>()
    .UseSqlServer(conn)
    .Options;

string otHost = config["Ot:Host"] ?? "127.0.0.1";
int otPort = int.TryParse(config["Ot:Port"], out var p) ? p : 502;
int pollSeconds = int.TryParse(config["PollingSeconds"], out var s) ? s : 1;

Console.WriteLine($"Integration BridgeService started — OT={otHost}:{otPort}");

while (true)
{
    try
    {
        using var db = new BridgeDbContext(dbOptions);

        var next = db.Orders.Where(o => o.Status == OrderStatus.New)
                            .OrderBy(o => o.CreatedAt)
                            .FirstOrDefault();

        if (next == null)
        {
            Thread.Sleep(1000);
            continue;
        }

        ModbusClient? client = null;
        try
        {
            client = new ModbusClient
            {
                IPAddress = otHost,
                Port = otPort
            };
            client.Connect();

            short id16 = (short)Math.Clamp(next.Id, short.MinValue, short.MaxValue);
            short qty16 = (short)Math.Clamp(next.Quantity, 0, short.MaxValue);

            client.WriteSingleRegister(0, id16);
            client.WriteSingleRegister(1, qty16);

            short nonce = (short)Random.Shared.Next(1, short.MaxValue);
            client.WriteSingleRegister(10, AuthKey);
            client.WriteSingleRegister(11, nonce);

            client.WriteSingleCoil(0, true);

            next.Status = OrderStatus.InProgress;
            db.SaveChanges();

            DateTime lastSeen = DateTime.UtcNow;
            int lastProduced = -1;

            while (true)
            {
                var producedVals = client.ReadInputRegisters(0, 1);
                int produced = producedVals[0];

                if (produced != lastProduced)
                {
                    lastProduced = produced;
                    lastSeen = DateTime.UtcNow;

                    db.ProductionLogs.Add(new ProductionLog
                    {
                        OrderId = next.Id,
                        ProducedCount = produced,
                        Message = "ProducedCount"
                    });
                    db.SaveChanges();

                    Console.WriteLine($"Order - Ordernumber: {next.Id} - Products produced - {produced}");
                }

                bool done = client.ReadDiscreteInputs(0, 1)[0];
                if (done)
                {
                    next.Status = OrderStatus.Completed;
                    db.SaveChanges();
                    Console.WriteLine($"Order - Ordernumber: {next.Id} - Order Completed.");
                    break;
                }

                if ((DateTime.UtcNow - lastSeen).TotalSeconds > 20)
                {
                    next.Status = OrderStatus.Failed;
                    next.LastError = "Timeout — no progress from OT";
                    db.SaveChanges();
                    Console.WriteLine($"Order - Ordernumber: {next.Id} failed: timeout");
                    break;
                }

                Thread.Sleep(pollSeconds * 1000);
            }
        }
        finally
        {
            try { if (client != null && client.Connected) client.Disconnect(); } catch { }
        }
    }
    catch (Exception ex)
    {
        try
        {
            using var db2 = new BridgeDbContext(dbOptions);
            var failing = db2.Orders.Where(o => o.Status == OrderStatus.InProgress)
                                    .OrderByDescending(o => o.CreatedAt)
                                    .FirstOrDefault();
            if (failing != null)
            {
                failing.Status = OrderStatus.Failed;
                failing.LastError = ex.Message;
                db2.SaveChanges();
            }
        }
        catch { }

        Console.WriteLine($"Integration error: {ex.Message}");
        Thread.Sleep(1000);
    }
}
