using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Admin.Products.DTOs
{
    public class AdminProductListItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Brand { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int VariantCount { get; set; }
        public int TotalStock { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
