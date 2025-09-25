using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITSystem.Models
{
    public class ProductionLog
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public int ProducedCount { get; set; }
        public string? Message { get; set; }
    }
}
