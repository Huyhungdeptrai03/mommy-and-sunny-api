using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Products.DTOs
{
    public class ProductDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Brand { get; set; }
        public int AgeRangeMinMonths { get; set; }
        public int AgeRangeMaxMonths { get; set; }

        public List<ProductImageDto> Images { get; set; } = new();
        public List<ProductVariantDto> Variants { get; set; } = new();
    }
}
