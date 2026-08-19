using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Orders.DTOs
{
    public class OrderItemDto
    {
        public string ProductName { get; set; } = string.Empty;
        public string? VariantDescription { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal Subtotal { get; set; }

        // Có giá trị nghĩa là sản phẩm này thuộc 1 đợt hàng đặt trước (xách tay)
        public string? PurchaseBatchName { get; set; }
    }
}
