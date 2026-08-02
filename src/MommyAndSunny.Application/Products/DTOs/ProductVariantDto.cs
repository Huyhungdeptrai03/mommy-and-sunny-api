using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Products.DTOs
{
    public class ProductVariantDto
    {
        public Guid Id { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string? Size { get; set; }
        public string? Color { get; set; }
        public decimal Price { get; set; }

        // Flash Sale — null nếu biến thể không đang giảm giá
        public decimal? CompareAtPrice { get; set; }
        public DateTime? SaleEndsAt { get; set; }

        // Chỉ trả số tồn kho khả dụng đã tính sẵn — KHÔNG lộ StockQuantity/ReservedQuantity thô
        public int AvailableQuantity { get; set; }
    }
}
