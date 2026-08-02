using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Admin.Categories.DTOs
{
    public class AdminCategoryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public Guid? ParentCategoryId { get; set; }
        public string? ParentCategoryName { get; set; }
        public string? ImageUrl { get; set; }
        public int ProductCount { get; set; }
    }
}
