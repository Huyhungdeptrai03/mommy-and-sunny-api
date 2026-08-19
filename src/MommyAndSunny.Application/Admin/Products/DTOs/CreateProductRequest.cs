using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Admin.Products.DTOs
{
    public class CreateProductRequest
    {
        public Guid CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Brand { get; set; }
        public int? AgeRangeMinMonths { get; set; }
        public int? AgeRangeMaxMonths { get; set; }

        // Có giá trị nghĩa là sản phẩm thuộc 1 đợt hàng đặt trước (xách tay)
        public Guid? PurchaseBatchId { get; set; }
    }
}
