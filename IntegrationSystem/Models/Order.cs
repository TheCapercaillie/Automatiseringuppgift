using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationSystem.Models
{
    public enum OrderStatus { New = 0, SentToOt = 1, InProgress = 2, Completed = 3, Failed = 4 }

    public class Order
    {
        public int Id { get; set; }
        public string Item { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public OrderStatus Status { get; set; } = OrderStatus.New;
        public string? LastError { get; set; }
    }
}
