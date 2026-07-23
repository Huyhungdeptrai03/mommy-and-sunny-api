using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Orders.DTOs
{
    public class OrderStatusHistoryDto
    {
        public string? FromStatus { get; set; }
        public string ToStatus { get; set; } = string.Empty;
        public string? Note { get; set; }
        public DateTime ChangedAt { get; set; }
    }
}
