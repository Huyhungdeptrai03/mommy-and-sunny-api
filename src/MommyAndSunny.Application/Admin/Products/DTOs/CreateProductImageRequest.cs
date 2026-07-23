using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Admin.Products.DTOs
{

    public class CreateProductImageRequest
    {
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsPrimary { get; set; } = false;
        public int DisplayOrder { get; set; } = 0;
    }
}
