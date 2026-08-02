using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Admin.Categories.DTOs
{
    public class UpdateCategoryRequest
    {
        public string Name { get; set; } = string.Empty;
        public Guid? ParentCategoryId { get; set; }
        public string? ImageUrl { get; set; }
    }
}
