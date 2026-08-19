using Microsoft.EntityFrameworkCore;
using MommyAndSunny.Application.Common.Interfaces;
using MommyAndSunny.Application.Coupons.DTOs;
using MommyAndSunny.Application.Coupons.Models;
using MommyAndSunny.Domain.Entities;
using MommyAndSunny.Domain.Enums;
using MommyAndSunny.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Coupons.Services
{
    public class CouponService : ICouponService
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public CouponService(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<ApplyCouponResponse> PreviewApplyAsync(
            string code, CancellationToken cancellationToken = default)
        {
            var userId = _currentUserService.UserId ?? throw new UnauthorizedException("Yêu cầu đăng nhập");

            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
            var cartItems = cart is null
                ? new List<CartItem>()
                : await _context.CartItems
                    .Where(i => i.CartId == cart.Id)
                    .Include(i => i.ProductVariant)
                        .ThenInclude(v => v.Product)
                    .ToListAsync(cancellationToken);

            if (cartItems.Count == 0)
            {
                return new ApplyCouponResponse { IsValid = false, Message = "Giỏ hàng đang trống" };
            }

            var result = await ValidateInternalAsync(code, userId, cartItems, cancellationToken);

            return new ApplyCouponResponse
            {
                IsValid = result.IsValid,
                Message = result.IsValid
                    ? $"Áp dụng thành công — giảm {result.DiscountAmount:N0}đ"
                    : result.ErrorMessage,
                DiscountAmount = result.DiscountAmount
            };
        }

        public Task<CouponValidationResult> ValidateForOrderAsync(
            string code, Guid userId, List<CartItem> cartItems, CancellationToken cancellationToken = default)
            => ValidateInternalAsync(code, userId, cartItems, cancellationToken);

        private async Task<CouponValidationResult> ValidateInternalAsync(
            string rawCode, Guid userId, List<CartItem> cartItems, CancellationToken cancellationToken)
        {
            var code = rawCode.Trim().ToUpperInvariant().Replace(" ", "");

            var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == code, cancellationToken);
            if (coupon is null)
            {
                return new CouponValidationResult { IsValid = false, ErrorMessage = "Mã giảm giá không tồn tại" };
            }

            if (!coupon.IsActive)
            {
                return new CouponValidationResult
                { IsValid = false, ErrorMessage = "Mã giảm giá này hiện đã bị vô hiệu hóa" };
            }

            var now = DateTime.UtcNow;
            if (now < coupon.StartDate || now > coupon.EndDate)
            {
                return new CouponValidationResult
                { IsValid = false, ErrorMessage = "Mã giảm giá đã hết hạn hoặc chưa đến thời gian áp dụng" };
            }

            if (coupon.TotalUsageLimit.HasValue && coupon.UsageCount >= coupon.TotalUsageLimit.Value)
            {
                return new CouponValidationResult
                { IsValid = false, ErrorMessage = "Mã giảm giá đã hết lượt sử dụng" };
            }

            // Tối đa 1 lần/khách hàng — đơn đã hủy KHÔNG tính, khách vẫn dùng lại được
            var alreadyUsed = await _context.Orders.AnyAsync(
                o => o.UserId == userId && o.CouponId == coupon.Id && o.Status != OrderStatus.Cancelled,
                cancellationToken);
            if (alreadyUsed)
            {
                return new CouponValidationResult
                { IsValid = false, ErrorMessage = "Bạn đã sử dụng mã giảm giá này rồi" };
            }

            // Tính số tiền đủ điều kiện áp mã — toàn đơn nếu không giới hạn danh mục,
            // ngược lại chỉ tính phần sản phẩm thuộc đúng danh mục của mã
            var eligibleAmount = coupon.CategoryId.HasValue
                ? cartItems
                    .Where(i => i.ProductVariant.Product.CategoryId == coupon.CategoryId.Value)
                    .Sum(i => i.ProductVariant.Price * i.Quantity)
                : cartItems.Sum(i => i.ProductVariant.Price * i.Quantity);

            if (eligibleAmount <= 0)
            {
                return new CouponValidationResult
                { IsValid = false, ErrorMessage = "Giỏ hàng không có sản phẩm thuộc phạm vi áp dụng của mã này" };
            }

            if (coupon.MinOrderValue.HasValue && eligibleAmount < coupon.MinOrderValue.Value)
            {
                return new CouponValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Cần tối thiểu {coupon.MinOrderValue.Value:N0}đ (thuộc phạm vi áp mã) để dùng mã này"
                };
            }

            decimal discount;
            if (coupon.DiscountType == CouponDiscountType.Percentage)
            {
                discount = eligibleAmount * (coupon.DiscountValue / 100);
                if (coupon.MaxDiscountAmount.HasValue)
                {
                    discount = Math.Min(discount, coupon.MaxDiscountAmount.Value);
                }
            }
            else
            {
                // FixedAmount — không bao giờ giảm nhiều hơn giá trị phần đủ điều kiện
                discount = Math.Min(coupon.DiscountValue, eligibleAmount);
            }

            return new CouponValidationResult { IsValid = true, DiscountAmount = discount, Coupon = coupon };
        }
    }

}
