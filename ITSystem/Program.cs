using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ITSystem.Data;
using ITSystem.Models;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

var conn = configuration.GetConnectionString("Default")
           ?? Environment.GetEnvironmentVariable("ConnectionStrings__Default")
           ?? throw new InvalidOperationException("Missing connection string 'Default'.");

var dbOptions = new DbContextOptionsBuilder<WarehouseDbContext>()
    .UseSqlServer(conn)
    .Options;

using (var ctx = new WarehouseDbContext(dbOptions))
{
    ctx.Database.Migrate();
}

using var db = new WarehouseDbContext(dbOptions);

bool loop = true;
while (loop)
{
    Console.WriteLine("\n=== IT: Beställningshanteraren ===");
    Console.WriteLine("1) Visa ordrar");
    Console.WriteLine("2) Ny order");
    Console.WriteLine("3) Avsluta");
    Console.Write("Val: ");
    var input = Console.ReadLine();

    switch (input)
    {
        case "1":
            var list = db.Orders.OrderByDescending(o => o.Id).ToList();
            if (!list.Any())
            {
                Console.WriteLine("Inga ordrar hittades.");
            }
            else
            {
                foreach (var o in list)
                    Console.WriteLine($"#{o.Id} {o.Item} x{o.Quantity} [{o.Status}] {o.CreatedAt:yyyy-MM-dd HH:mm}");
            }
            break;

        case "2":
            Console.Write("Artikel: ");
            var item = (Console.ReadLine() ?? string.Empty).Trim();
            Console.Write("Antal: ");
            var qtyInput = Console.ReadLine();
            if (!int.TryParse(qtyInput, out var qty) || qty <= 0)
            {
                Console.WriteLine("Ogiltigt antal.");
                break;
            }

            var order = new Order { Item = item, Quantity = qty };
            db.Orders.Add(order);
            db.SaveChanges();
            Console.WriteLine($"Order skapad med id {order.Id}");
            break;

        case "3":
            loop = false;
            Console.WriteLine("Stänger ner IT-delen...");
            break;

        default:
            Console.WriteLine("Ogiltigt val.");
            break;
    }
}
