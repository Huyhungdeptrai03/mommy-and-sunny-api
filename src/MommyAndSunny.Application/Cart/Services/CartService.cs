using Microsoft.EntityFrameworkCore;
using MommyAndSunny.Application.Cart.DTOs;
using MommyAndSunny.Application.Common.Interfaces;
using MommyAndSunny.Domain.Entities;
using MommyAndSunny.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Cart.Services
{
    public class CartService : ICartService
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public CartService(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<CartDto> GetCartAsync(CancellationToken cancellationToken = default)
        {
            var cart = await GetOrCreateCartAsync(cancellationToken);
            return await BuildCartDtoAsync(cart.Id, cancellationToken);
        }

        public async Task<CartDto> AddItemAsync(AddCartItemRequest request, CancellationToken cancellationToken = default)
        {
            if (request.Quantity <= 0)
            {
                throw new DomainException("Số lượng phải lớn hơn 0");
            }

            var variant = await _context.ProductVariants
                .Include(v => v.Product)
                .FirstOrDefaultAsync(v => v.Id == request.ProductVariantId, cancellationToken);

            if (variant is null)
            {
                throw new NotFoundException("biến thể sản phẩm", request.ProductVariantId);
            }

            var cart = await GetOrCreateCartAsync(cancellationToken);

            // Ràng buộc nghiệp vụ: mỗi biến thể chỉ 1 dòng trong giỏ — nếu đã có, cộng dồn số lượng
            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(i => i.CartId == cart.Id && i.ProductVariantId == request.ProductVariantId, cancellationToken);

            var newTotalQuantity = (existingItem?.Quantity ?? 0) + request.Quantity;

            if (newTotalQuantity > variant.AvailableQuantity)
            {
                throw new InsufficientStockException(GetVariantDisplayName(variant), variant.AvailableQuantity, newTotalQuantity);
            }

            if (existingItem is not null)
            {
                existingItem.Quantity = newTotalQuantity;
            }
            else
            {
                _context.CartItems.Add(new CartItem
                {
                    CartId = cart.Id,
                    ProductVariantId = request.ProductVariantId,
                    Quantity = request.Quantity
                });
            }

            await _context.SaveChangesAsync(cancellationToken);

            return await BuildCartDtoAsync(cart.Id, cancellationToken);
        }

        public async Task<CartDto> UpdateItemAsync(Guid itemId, UpdateCartItemRequest request, CancellationToken cancellationToken = default)
        {
            if (request.Quantity <= 0)
            {
                throw new DomainException("Số lượng phải lớn hơn 0");
            }

            var cart = await GetOrCreateCartAsync(cancellationToken);

            var item = await _context.CartItems
                .Include(i => i.ProductVariant)
                    .ThenInclude(v => v.Product)
                // Điều kiện CartId = cart.Id đảm bảo user A không thể sửa item trong giỏ của user B
                .FirstOrDefaultAsync(i => i.Id == itemId && i.CartId == cart.Id, cancellationToken);

            if (item is null)
            {
                throw new NotFoundException("dòng giỏ hàng", itemId);
            }

            if (request.Quantity > item.ProductVariant.AvailableQuantity)
            {
                throw new InsufficientStockException(
                    GetVariantDisplayName(item.ProductVariant), item.ProductVariant.AvailableQuantity, request.Quantity);
            }

            item.Quantity = request.Quantity;
            await _context.SaveChangesAsync(cancellationToken);

            return await BuildCartDtoAsync(cart.Id, cancellationToken);
        }

        public async Task RemoveItemAsync(Guid itemId, CancellationToken cancellationToken = default)
        {
            var cart = await GetOrCreateCartAsync(cancellationToken);

            var item = await _context.CartItems
                .FirstOrDefaultAsync(i => i.Id == itemId && i.CartId == cart.Id, cancellationToken);

            if (item is null)
            {
                throw new NotFoundException("dòng giỏ hàng", itemId);
            }

            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task<Domain.Entities.Cart> GetOrCreateCartAsync(CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId
                ?? throw new UnauthorizedException("Yêu cầu đăng nhập");

            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
            if (cart is not null)
            {
                return cart;
            }

            // Lần đầu thêm vào giỏ — tự động tạo Cart cho user, không bắt phải gọi API tạo giỏ riêng
            cart = new Domain.Entities.Cart { UserId = userId };
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync(cancellationToken);

            return cart;
        }

        private async Task<CartDto> BuildCartDtoAsync(Guid cartId, CancellationToken cancellationToken)
        {
            var items = await _context.CartItems
                .Where(i => i.CartId == cartId)
                .Include(i => i.ProductVariant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.PurchaseBatch)
                .Include(i => i.ProductVariant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.Images)
                .ToListAsync(cancellationToken);

            var itemDtos = items.Select(i => new CartItemDto
            {
                ItemId = i.Id,
                ProductVariantId = i.ProductVariantId,
                ProductName = i.ProductVariant.Product.Name,
                ImageUrl = i.ProductVariant.Product.Images
                    .Where(img => img.IsPrimary)
                    .Select(img => img.ImageUrl)
                    .FirstOrDefault(),
                Size = i.ProductVariant.Size,
                Color = i.ProductVariant.Color,
                UnitPrice = i.ProductVariant.Price,
                Quantity = i.Quantity,
                AvailableQuantity = i.ProductVariant.AvailableQuantity,
                IsAvailable = i.Quantity <= i.ProductVariant.AvailableQuantity,
                Warning = i.Quantity > i.ProductVariant.AvailableQuantity
                    ? $"Chỉ còn {i.ProductVariant.AvailableQuantity} sản phẩm trong kho"
                    : null,
                PurchaseBatchName = i.ProductVariant.Product.PurchaseBatch?.Name
            }).ToList();

            return new CartDto
            {
                Items = itemDtos,
                // Chỉ cộng dồn dòng còn hợp lệ — tránh khách checkout theo giá của sản phẩm đã hết hàng
                SubTotal = itemDtos.Where(i => i.IsAvailable).Sum(i => i.UnitPrice * i.Quantity)
            };
        }

        private static string GetVariantDisplayName(ProductVariant variant)
        {
            var parts = new List<string> { variant.Product.Name };
            if (!string.IsNullOrEmpty(variant.Size))
            {
                parts.Add($"Size {variant.Size}");
            }
            return string.Join(" - ", parts);
        }
    }
}