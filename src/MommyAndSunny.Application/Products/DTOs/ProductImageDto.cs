using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Products.DTOs
{
    public class ProductImageDto
    {
        public string Url { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
    }
}
