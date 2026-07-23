using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Cart.DTOs
{
    public class CartDto
    {
        public List<CartItemDto> Items { get; set; } = new();

        // Chỉ tính tổng những dòng còn hợp lệ (IsAvailable = true) —
        // tránh cho khách checkout nhầm với giá của sản phẩm đã hết hàng
        public decimal SubTotal { get; set; }
    }
}
