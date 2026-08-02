using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Admin.Products.DTOs
{
    public class UpdateProductVariantRequest
    {
        public string Sku { get; set; } = string.Empty;
        public string? Size { get; set; }
        public string? Color { get; set; }
        public decimal Price { get; set; }

        // Nhập lại TOÀN BỘ tồn kho thực (VD: admin kiểm kê/nhập thêm hàng) —
        // KHÔNG liên quan đến ReservedQuantity (số đang bị giữ chỗ bởi đơn Pending), 2 giá trị độc lập
        public int StockQuantity { get; set; }


        // Flash Sale — không bắt buộc, để trống (null) nghĩa là không giảm giá
        public decimal? CompareAtPrice { get; set; }
        public DateTime? SaleEndsAt { get; set; }
    }
}
