using Microsoft.EntityFrameworkCore;
using MommyAndSunny.Application.Common.Interfaces;
using MommyAndSunny.Application.Coupons.DTOs;
using MommyAndSunny.Domain.Entities;
using MommyAndSunny.Domain.Enums;
using MommyAndSunny.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Coupons.Services
{
    public class AdminCouponService : IAdminCouponService
    {
        private readonly IApplicationDbContext _context;

        public AdminCouponService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<AdminCouponDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var coupons = await _context.Coupons
                .Include(c => c.Category)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync(cancellationToken);

            return coupons.Select(c => MapToDto(c)).ToList();
        }

        public async Task<AdminCouponDto> CreateAsync(
            CreateCouponRequest request, CancellationToken cancellationToken = default)
        {
            var code = NormalizeCode(request.Code);
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new DomainException("Mã giảm giá không được để trống");
            }

            var codeExists = await _context.Coupons.AnyAsync(c => c.Code == code, cancellationToken);
            if (codeExists)
            {
                throw new ConflictException($"Mã giảm giá \"{code}\" đã tồn tại");
            }

            var discountType = ParseDiscountType(request.DiscountType);
            ValidateDiscountValue(discountType, request.DiscountValue);

            if (request.EndDate <= request.StartDate)
            {
                throw new DomainException("Ngày kết thúc phải sau ngày bắt đầu");
            }

            if (request.CategoryId.HasValue)
            {
                var categoryExists = await _context.Categories
                    .AnyAsync(c => c.Id == request.CategoryId.Value, cancellationToken);
                if (!categoryExists)
                {
                    throw new NotFoundException("danh mục", request.CategoryId.Value);
                }
            }

            var coupon = new Coupon
            {
                Code = code,
                DiscountType = discountType,
                DiscountValue = request.DiscountValue,
                MaxDiscountAmount = request.MaxDiscountAmount,
                MinOrderValue = request.MinOrderValue,
                CategoryId = request.CategoryId,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                TotalUsageLimit = request.TotalUsageLimit,
                IsActive = true
            };

            _context.Coupons.Add(coupon);
            await _context.SaveChangesAsync(cancellationToken);

            var categoryName = request.CategoryId.HasValue
                ? await _context.Categories
                    .Where(c => c.Id == request.CategoryId.Value)
                    .Select(c => c.Name)
                    .FirstOrDefaultAsync(cancellationToken)
                : null;

            return MapToDto(coupon, categoryName);
        }

        public async Task<AdminCouponDto> UpdateAsync(
            Guid id, UpdateCouponRequest request, CancellationToken cancellationToken = default)
        {
            var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
                ?? throw new NotFoundException("mã giảm giá", id);

            var discountType = ParseDiscountType(request.DiscountType);
            ValidateDiscountValue(discountType, request.DiscountValue);

            if (request.EndDate <= request.StartDate)
            {
                throw new DomainException("Ngày kết thúc phải sau ngày bắt đầu");
            }

            if (request.CategoryId.HasValue)
            {
                var categoryExists = await _context.Categories
                    .AnyAsync(c => c.Id == request.CategoryId.Value, cancellationToken);
                if (!categoryExists)
                {
                    throw new NotFoundException("danh mục", request.CategoryId.Value);
                }
            }

            // CỐ Ý không cho đổi Code sau khi tạo — mã đã có thể được chia sẻ/ghi nhớ,
            // đổi giữa chừng dễ gây nhầm lẫn cho khách đã biết mã cũ
            coupon.DiscountType = discountType;
            coupon.DiscountValue = request.DiscountValue;
            coupon.MaxDiscountAmount = request.MaxDiscountAmount;
            coupon.MinOrderValue = request.MinOrderValue;
            coupon.CategoryId = request.CategoryId;
            coupon.StartDate = request.StartDate;
            coupon.EndDate = request.EndDate;
            coupon.TotalUsageLimit = request.TotalUsageLimit;
            coupon.IsActive = request.IsActive;

            await _context.SaveChangesAsync(cancellationToken);

            var categoryName = request.CategoryId.HasValue
                ? await _context.Categories
                    .Where(c => c.Id == request.CategoryId.Value)
                    .Select(c => c.Name)
                    .FirstOrDefaultAsync(cancellationToken)
                : null;

            return MapToDto(coupon, categoryName);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
                ?? throw new NotFoundException("mã giảm giá", id);

            // Cho phép xóa TỰ DO kể cả khi đã có lịch sử sử dụng — vì Order.CouponCode đã
            // snapshot sẵn, đơn cũ vẫn hiển thị đúng thông tin mã đã dùng dù Coupon gốc bị xóa
            // (FK Order.CouponId cấu hình SetNull, không rơi vào lỗi ràng buộc khóa ngoại)
            _context.Coupons.Remove(coupon);
            await _context.SaveChangesAsync(cancellationToken);
        }

        private static string NormalizeCode(string code) =>
            code.Trim().ToUpperInvariant().Replace(" ", "");

        private static CouponDiscountType ParseDiscountType(string value)
        {
            if (!Enum.TryParse<CouponDiscountType>(value, out var type))
            {
                throw new DomainException("Loại giảm giá không hợp lệ");
            }
            return type;
        }

        private static void ValidateDiscountValue(CouponDiscountType type, decimal value)
        {
            if (value <= 0)
            {
                throw new DomainException("Giá trị giảm giá phải lớn hơn 0");
            }
            if (type == CouponDiscountType.Percentage && value > 100)
            {
                throw new DomainException("Giảm giá theo % không được vượt quá 100");
            }
        }

        private static AdminCouponDto MapToDto(Coupon coupon, string? categoryNameOverride = null) => new()
        {
            Id = coupon.Id,
            Code = coupon.Code,
            DiscountType = coupon.DiscountType.ToString(),
            DiscountValue = coupon.DiscountValue,
            MaxDiscountAmount = coupon.MaxDiscountAmount,
            MinOrderValue = coupon.MinOrderValue,
            CategoryId = coupon.CategoryId,
            CategoryName = categoryNameOverride ?? coupon.Category?.Name,
            StartDate = coupon.StartDate,
            EndDate = coupon.EndDate,
            TotalUsageLimit = coupon.TotalUsageLimit,
            UsageCount = coupon.UsageCount,
            IsActive = coupon.IsActive
        };
    }
}
