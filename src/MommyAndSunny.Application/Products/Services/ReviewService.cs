using Microsoft.EntityFrameworkCore;
using MommyAndSunny.Application.Common.Interfaces;
using MommyAndSunny.Application.Products.DTOs;
using MommyAndSunny.Domain.Entities;
using MommyAndSunny.Domain.Enums;
using MommyAndSunny.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Products.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IApplicationDbContext _context;

        public ReviewService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ProductReviewsDto> GetReviewsAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Where(r => r.ProductId == productId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync(cancellationToken);

            var summary = new ReviewSummaryDto
            {
                TotalCount = reviews.Count,
                AverageRating = reviews.Count > 0 ? Math.Round(reviews.Average(r => r.Rating), 1) : 0,
                RatingBreakdown = Enumerable.Range(1, 5)
                    .ToDictionary(star => star, star => reviews.Count(r => r.Rating == star))
            };

            return new ProductReviewsDto
            {
                Summary = summary,
                Items = reviews.Select(r => new ReviewDto
                {
                    Id = r.Id,
                    UserName = MaskName(r.User.FullName),
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt
                }).ToList()
            };
        }

        public async Task<bool> CanUserReviewAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default)
        {
            // Đã đánh giá rồi thì không cho đánh giá lại (unique index ở DB cũng chặn, nhưng
            // kiểm tra sớm ở đây để trả lỗi/thông báo rõ ràng hơn cho FE)
            var alreadyReviewed = await _context.Reviews
                .AnyAsync(r => r.UserId == userId && r.ProductId == productId, cancellationToken);
            if (alreadyReviewed) return false;

            // Chỉ cho đánh giá nếu đã từng có đơn hàng Ở TRẠNG THÁI ĐÃ GIAO chứa sản phẩm này —
            // đây là "verified purchase", tránh review ảo từ người chưa thực sự mua/nhận hàng.
            // Join thủ công qua OrderItems -> ProductVariants (thay vì dùng navigation property)
            // để tránh phụ thuộc vào việc Variant navigation có bị null hay không
            var hasPurchased = await (
                from o in _context.Orders
                join oi in _context.OrderItems on o.Id equals oi.OrderId
                join v in _context.ProductVariants on oi.ProductVariantId equals v.Id
                where o.UserId == userId && o.Status == OrderStatus.Delivered && v.ProductId == productId
                select o.Id
            ).AnyAsync(cancellationToken);

            return hasPurchased;
        }

        public async Task<ReviewDto> CreateReviewAsync(
            Guid userId, Guid productId, CreateReviewRequest request, CancellationToken cancellationToken = default)
        {
            if (request.Rating is < 1 or > 5)
            {
                throw new DomainException("Số sao đánh giá phải từ 1 đến 5");
            }

            var canReview = await CanUserReviewAsync(userId, productId, cancellationToken);
            if (!canReview)
            {
                throw new DomainException(
                    "Bạn cần mua và nhận sản phẩm này trước khi đánh giá, hoặc đã đánh giá rồi");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
                ?? throw new UnauthorizedException("Yêu cầu đăng nhập");

            var review = new Review
            {
                ProductId = productId,
                UserId = userId,
                Rating = request.Rating,
                Comment = request.Comment
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync(cancellationToken);

            return new ReviewDto
            {
                Id = review.Id,
                UserName = MaskName(user.FullName),
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedAt = review.CreatedAt
            };
        }

        // Ẩn bớt họ tên đầy đủ khi hiển thị công khai — chỉ hiện họ + chữ cái đầu tên,
        // bảo vệ quyền riêng tư khách hàng (VD: "Nguyễn Văn An" -> "Nguyễn N.")
        private static string MaskName(string fullName)
        {
            var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length <= 1) return fullName;

            var firstName = parts[0];
            var lastInitial = parts[^1][0];
            return $"{firstName} {lastInitial}.";
        }
    }
}
