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
int pollSeconds = 1;

Console.WriteLine($"BridgeService started — OT={otHost}:{otPort}");

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

            client.WriteSingleCoil(0, true);

            next.Status = OrderStatus.InProgress;
            db.SaveChanges();


        }
        finally
        {
            try { if (client != null && client.Connected) client.Disconnect(); } catch { }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Integration loop error: {ex.Message}");
        Thread.Sleep(1000);
    }
}
