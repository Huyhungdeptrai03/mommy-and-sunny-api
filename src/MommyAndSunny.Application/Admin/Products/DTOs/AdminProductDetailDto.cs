using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Admin.Products.DTOs
{
    public class AdminProductDetailDto
    {
        public Guid Id { get; set; }
        public Guid CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Brand { get; set; }
        public int AgeRangeMinMonths { get; set; }
        public int AgeRangeMaxMonths { get; set; }
        public string Status { get; set; } = string.Empty;

        public List<ProductImageAdminDto> Images { get; set; } = new();
        public List<ProductVariantAdminDto> Variants { get; set; } = new();
    }
}
