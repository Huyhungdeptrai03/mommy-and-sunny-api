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
        public int? AgeRangeMinMonths { get; set; }
        public int? AgeRangeMaxMonths { get; set; }

        // Null nếu chưa có đánh giá nào
        public double? AverageRating { get; set; }
        public int ReviewCount { get; set; }

        // Có giá trị nghĩa là sản phẩm này đang đặt trước (xách tay), chưa có hàng thật
        public string? PurchaseBatchName { get; set; }
        public DateTime? EstimatedArrivalDate { get; set; }

        public List<ProductImageDto> Images { get; set; } = new();
        public List<ProductSpecificationDto> Specifications { get; set; } = new();
        public List<ProductVariantDto> Variants { get; set; } = new();
    }

}
