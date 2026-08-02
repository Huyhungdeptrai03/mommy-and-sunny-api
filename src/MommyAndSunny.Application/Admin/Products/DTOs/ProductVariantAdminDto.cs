using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Admin.Products.DTOs
{
    public class ProductVariantAdminDto
    {
        public Guid Id { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string? Size { get; set; }
        public string? Color { get; set; }
        public decimal Price { get; set; }

        // Flash Sale
        public decimal? CompareAtPrice { get; set; }
        public DateTime? SaleEndsAt { get; set; }

        public int StockQuantity { get; set; }
        public int ReservedQuantity { get; set; }
        public int AvailableQuantity { get; set; }
    }
}
