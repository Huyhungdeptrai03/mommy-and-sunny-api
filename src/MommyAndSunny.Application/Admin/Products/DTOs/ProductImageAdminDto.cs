using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Admin.Products.DTOs
{
    public class ProductImageAdminDto
    {
        public Guid Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public int DisplayOrder { get; set; }
    }
}
