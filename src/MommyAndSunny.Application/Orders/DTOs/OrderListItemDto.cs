using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Orders.DTOs
{
    public class OrderListItemDto
    {
        public string OrderCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
