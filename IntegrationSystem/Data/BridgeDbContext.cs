using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using IntegrationSystem.Models;

namespace IntegrationSystem.Data
{
    public class BridgeDbContext : DbContext
    {
        public BridgeDbContext(DbContextOptions<BridgeDbContext> options) : base(options) { }

        public DbSet<Order> Orders => Set<Order>();
        public DbSet<ProductionLog> ProductionLogs => Set<ProductionLog>();
    }
}
