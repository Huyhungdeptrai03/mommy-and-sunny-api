using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Products.DTOs
{
    public class ProductListItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Brand { get; set; }
        public int? AgeRangeMinMonths { get; set; }
        public int? AgeRangeMaxMonths { get; set; }
        public string? PrimaryImageUrl { get; set; }

        // Có giá trị nghĩa là sản phẩm này đang đặt trước (xách tay)
        public string? PurchaseBatchName { get; set; }

        // Khoảng giá — vì giá thực tế nằm ở cấp biến thể, sản phẩm không có 1 giá cố định
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
    }
}
