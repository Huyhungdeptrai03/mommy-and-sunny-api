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

        // Chỉ trả số tồn kho khả dụng đã tính sẵn — KHÔNG lộ StockQuantity/ReservedQuantity thô
        // ra API công khai, theo đúng quyết định thiết kế đã thống nhất ở API contract
        public int AvailableQuantity { get; set; }
    }
}
