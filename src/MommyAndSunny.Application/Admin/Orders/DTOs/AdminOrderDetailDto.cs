using MommyAndSunny.Application.Orders.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Admin.Orders.DTOs
{
    public class AdminOrderDetailDto
    {
        public string OrderCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;

        public string RecipientName { get; set; } = string.Empty;
        public string RecipientPhone { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;

        public decimal SubTotal { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public string? CouponCode { get; set; }
        public decimal TotalAmount { get; set; }

        public decimal? DepositAmount { get; set; }
        public string DepositPaymentStatus { get; set; } = string.Empty;

        // Đơn có sản phẩm đặt trước VÀ giá trị vượt ngưỡng cấu hình -> bắt buộc phải có
        // cọc đã thu trước khi chuyển sang "Đang mua hàng". FE dùng để hiện cảnh báo sớm,
        // nhưng ràng buộc thật sự nằm ở Backend (UpdateOrderStatusAsync)
        public bool RequiresDeposit { get; set; }

        public string? CustomerNote { get; set; }
        public DateTime CreatedAt { get; set; }

        // Tái sử dụng DTO đã có từ module Orders (customer-facing) — tránh viết trùng
        public List<OrderItemDto> Items { get; set; } = new();
        public List<OrderStatusHistoryDto> StatusHistory { get; set; } = new();
    }

}
