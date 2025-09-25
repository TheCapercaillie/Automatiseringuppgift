using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ITSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace ITSystem.Data
{
    public class WarehouseDbContext : DbContext
    {
        public WarehouseDbContext(DbContextOptions<WarehouseDbContext> options) : base(options) { }

        public DbSet<Order> Orders => Set<Order>();
        public DbSet<ProductionLog> ProductionLogs => Set<ProductionLog>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Order>(e =>
            {
                e.Property(x => x.Item).HasMaxLength(100).IsRequired();
                e.HasIndex(x => x.Status);
            });

            builder.Entity<ProductionLog>(e =>
            {
                e.HasIndex(p => p.OrderId);
            });
        }
    }
}
