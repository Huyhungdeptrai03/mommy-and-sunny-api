using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Categories.DTOs
{

    public class CategoryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }

        // Đệ quy — danh mục con lồng bên trong danh mục cha, FE dựng menu trực tiếp từ đây
        public List<CategoryDto> Children { get; set; } = new();
    }
}
