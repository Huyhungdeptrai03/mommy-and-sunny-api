using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Admin.Products.DTOs
{
    public class ProductVariantAdminDto
    {
        public Guid Id { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string? Size { get; set; }
        public string? Color { get; set; }
        public decimal Price { get; set; }

        // Khác với API công khai (chỉ trả AvailableQuantity) — admin cần thấy đủ cả 3 chỉ số
        // để hiểu rõ đang có bao nhiêu hàng thật, bao nhiêu đang bị giữ chỗ bởi đơn Pending
        public int StockQuantity { get; set; }
        public int ReservedQuantity { get; set; }
        public int AvailableQuantity { get; set; }
    }
}
