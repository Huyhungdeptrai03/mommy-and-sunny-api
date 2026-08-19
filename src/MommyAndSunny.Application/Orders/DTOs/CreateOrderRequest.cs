using MommyAndSunny.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Orders.DTOs
{
    public class CreateOrderRequest
    {
        public Guid AddressId { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string? CouponCode { get; set; }
        public string? CustomerNote { get; set; }

        // CỐ Ý không có field Items — BE luôn tự lấy từ giỏ hàng server-side,
        // không bao giờ tin giá/số lượng do client gửi lên (nguyên tắc bảo mật đã thống nhất)
    }
}
