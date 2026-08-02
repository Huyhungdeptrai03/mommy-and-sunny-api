using Microsoft.EntityFrameworkCore;
using MommyAndSunny.Application.Admin.Reviews.DTOs;
using MommyAndSunny.Application.Common.Interfaces;
using MommyAndSunny.Application.Common.Models;
using MommyAndSunny.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Admin.Reviews.Services
{
    public class AdminReviewService : IAdminReviewService
    {
        private readonly IApplicationDbContext _context;

        public AdminReviewService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<AdminReviewDto>> GetReviewsAsync(
            AdminReviewFilterRequest filter, CancellationToken cancellationToken = default)
        {
            var query = _context.Reviews
                .Include(r => r.Product)
                .Include(r => r.User)
                .AsQueryable();

            if (filter.Rating.HasValue)
            {
                query = query.Where(r => r.Rating == filter.Rating.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var reviews = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(r => new AdminReviewDto
                {
                    Id = r.Id,
                    ProductId = r.ProductId,
                    ProductName = r.Product.Name,
                    UserFullName = r.User.FullName,
                    UserEmail = r.User.Email,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync(cancellationToken);

            return new PagedResult<AdminReviewDto>
            {
                Items = reviews,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }

        public async Task DeleteReviewAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var review = await _context.Reviews.FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
                ?? throw new NotFoundException("đánh giá", id.ToString());

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
