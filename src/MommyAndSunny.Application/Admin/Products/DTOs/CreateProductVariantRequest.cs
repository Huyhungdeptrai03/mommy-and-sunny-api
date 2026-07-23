using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Admin.Products.DTOs
{
    public class CreateProductVariantRequest
    {
        public string Sku { get; set; } = string.Empty;
        public string? Size { get; set; }
        public string? Color { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
    }
}
