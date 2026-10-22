using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Cart.DTOs
{
    public class CartItemDto
    {
        public Guid ItemId { get; set; }
        public Guid ProductVariantId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string? Size { get; set; }
        public string? Color { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }

        // Kiểm tra lại tồn kho real-time mỗi lần load giỏ hàng — theo đúng nghiệp vụ đã thống nhất
        public int AvailableQuantity { get; set; }
        public bool IsAvailable { get; set; }
        public string? Warning { get; set; }

        // Có giá trị nghĩa là sản phẩm này thuộc đợt hàng đặt trước (xách tay) —
        // FE dùng để ẩn lựa chọn SePay ở Checkout khi giỏ hàng có sản phẩm loại này
        public string? PurchaseBatchName { get; set; }
    }
}
