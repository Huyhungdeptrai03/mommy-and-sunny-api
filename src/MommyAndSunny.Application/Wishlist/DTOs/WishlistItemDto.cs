using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Wishlist.DTOs
{
    public class WishlistItemDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductSlug { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public string? PurchaseBatchName { get; set; }

        // Sản phẩm có thể đã bị Admin chuyển "Ngừng bán" sau khi khách thêm vào wishlist —
        // vẫn hiện trong danh sách (khách có thể muốn theo dõi) nhưng đánh dấu rõ không mua được nữa
        public bool IsAvailable { get; set; }
        public DateTime AddedAt { get; set; }
    }
}
