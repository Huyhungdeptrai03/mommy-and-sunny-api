using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Products.DTOs
{
    public class FlashSaleItemDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductSlug { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public Guid VariantId { get; set; }
        public decimal Price { get; set; }
        public decimal CompareAtPrice { get; set; }
        public int DiscountPercent { get; set; }
        public DateTime SaleEndsAt { get; set; }
        public int AvailableQuantity { get; set; }
    }
}
