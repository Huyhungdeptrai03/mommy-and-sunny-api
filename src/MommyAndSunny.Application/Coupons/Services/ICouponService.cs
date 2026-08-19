using MommyAndSunny.Application.Coupons.DTOs;
using MommyAndSunny.Application.Coupons.Models;
using MommyAndSunny.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Coupons.Services
{
    public interface ICouponService
    {
        // Dùng ở Checkout — khách nhập mã, xem trước số tiền giảm mà KHÔNG ghi nhận lượt dùng
        Task<ApplyCouponResponse> PreviewApplyAsync(string code, CancellationToken cancellationToken = default);

        // Dùng ở OrderService.CreateOrderAsync — validate LẠI TỪ ĐẦU ở server, không bao giờ
        // tin số tiền giảm giá do client tính/gửi lên (nguyên tắc bảo mật đã theo suốt dự án)
        Task<CouponValidationResult> ValidateForOrderAsync(
            string code, Guid userId, List<CartItem> cartItems, CancellationToken cancellationToken = default);
    }
}
