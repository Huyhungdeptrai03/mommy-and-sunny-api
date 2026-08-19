using Microsoft.EntityFrameworkCore;
using MommyAndSunny.Application.Common.Interfaces;
using MommyAndSunny.Application.Wishlist.DTOs;
using MommyAndSunny.Domain.Entities;
using MommyAndSunny.Domain.Enums;
using MommyAndSunny.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Wishlist.Services
{
    public class WishlistService : IWishlistService
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public WishlistService(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<List<WishlistItemDto>> GetMyWishlistAsync(CancellationToken cancellationToken = default)
        {
            var userId = _currentUserService.UserId ?? throw new UnauthorizedException("Yêu cầu đăng nhập");

            var items = await _context.WishlistItems
                .Where(w => w.UserId == userId)
                .Include(w => w.Product)
                    .ThenInclude(p => p.Images)
                .Include(w => w.Product)
                    .ThenInclude(p => p.Variants)
                .Include(w => w.Product)
                    .ThenInclude(p => p.PurchaseBatch)
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync(cancellationToken);

            return items.Select(w => new WishlistItemDto
            {
                ProductId = w.ProductId,
                ProductName = w.Product.Name,
                ProductSlug = w.Product.Slug,
                ImageUrl = w.Product.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl
                    ?? w.Product.Images.FirstOrDefault()?.ImageUrl,
                MinPrice = w.Product.Variants.Count > 0 ? w.Product.Variants.Min(v => v.Price) : 0,
                MaxPrice = w.Product.Variants.Count > 0 ? w.Product.Variants.Max(v => v.Price) : 0,
                PurchaseBatchName = w.Product.PurchaseBatch?.Name,
                IsAvailable = w.Product.Status == ProductStatus.Published,
                AddedAt = w.CreatedAt
            }).ToList();
        }

        public async Task AddAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            var userId = _currentUserService.UserId ?? throw new UnauthorizedException("Yêu cầu đăng nhập");

            var productExists = await _context.Products.AnyAsync(p => p.Id == productId, cancellationToken);
            if (!productExists)
            {
                throw new NotFoundException("sản phẩm", productId);
            }

            var alreadyExists = await _context.WishlistItems
                .AnyAsync(w => w.UserId == userId && w.ProductId == productId, cancellationToken);
            if (alreadyExists)
            {
                // Đã có sẵn -> coi như thành công luôn, không cần báo lỗi (idempotent,
                // tránh lỗi vặt nếu khách bấm nhanh 2 lần hoặc đồng bộ nhiều tab)
                return;
            }

            _context.WishlistItems.Add(new WishlistItem { UserId = userId, ProductId = productId });
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task RemoveAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            var userId = _currentUserService.UserId ?? throw new UnauthorizedException("Yêu cầu đăng nhập");

            var item = await _context.WishlistItems
                .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId, cancellationToken);

            // Không có sẵn -> cũng coi như thành công (idempotent), không cần báo lỗi 404
            if (item is not null)
            {
                _context.WishlistItems.Remove(item);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
    }

}
