using Microsoft.EntityFrameworkCore;
using MommyAndSunny.Application.Common.Interfaces;
using MommyAndSunny.Application.Common.Models;
using MommyAndSunny.Application.Products.DTOs;
using MommyAndSunny.Domain.Enums;
using MommyAndSunny.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Products.Services
{
    public class ProductService : IProductService
    {
        private readonly IApplicationDbContext _context;

        public ProductService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<ProductListItemDto>> GetProductsAsync(
            ProductFilterRequest filter,
            CancellationToken cancellationToken = default)
        {
            // Chỉ trả sản phẩm Published — quy tắc bắt buộc cho API công khai,
            // Draft/Discontinued không bao giờ được lộ ra ngoài storefront
            var query = _context.Products
                .Where(p => p.Status == ProductStatus.Published)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.CategorySlug))
            {
                query = query.Where(p => p.Category.Slug == filter.CategorySlug);
            }

            if (filter.AgeMonths.HasValue)
            {
                query = query.Where(p =>
                    filter.AgeMonths.Value >= p.AgeRangeMinMonths &&
                    filter.AgeMonths.Value <= p.AgeRangeMaxMonths);
            }

            if (filter.MinPrice.HasValue)
            {
                query = query.Where(p => p.Variants.Any(v => v.Price >= filter.MinPrice.Value));
            }

            if (filter.MaxPrice.HasValue)
            {
                query = query.Where(p => p.Variants.Any(v => v.Price <= filter.MaxPrice.Value));
            }

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                // Đơn giản hóa cho MVP (Contains cơ bản) — tìm kiếm full-text nâng cao để Tier 2
                query = query.Where(p => p.Name.Contains(filter.Search));
            }

            query = filter.SortBy switch
            {
                ProductSortBy.PriceAsc => query.OrderBy(p => p.Variants.Min(v => v.Price)),
                ProductSortBy.PriceDesc => query.OrderByDescending(p => p.Variants.Min(v => v.Price)),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(p => new ProductListItemDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Slug = p.Slug,
                    Brand = p.Brand,
                    AgeRangeMinMonths = p.AgeRangeMinMonths,
                    AgeRangeMaxMonths = p.AgeRangeMaxMonths,
                    PrimaryImageUrl = p.Images
                        .Where(i => i.IsPrimary)
                        .Select(i => i.ImageUrl)
                        .FirstOrDefault(),
                    MinPrice = p.Variants.Min(v => v.Price),
                    MaxPrice = p.Variants.Max(v => v.Price)
                })
                .ToListAsync(cancellationToken);

            return new PagedResult<ProductListItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }

        public async Task<ProductDto> GetProductBySlugAsync(string slug, CancellationToken cancellationToken = default)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Slug == slug && p.Status == ProductStatus.Published, cancellationToken);

            if (product is null)
            {
                throw new NotFoundException("sản phẩm", slug);
            }

            return new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Slug = product.Slug,
                Description = product.Description,
                Brand = product.Brand,
                AgeRangeMinMonths = product.AgeRangeMinMonths,
                AgeRangeMaxMonths = product.AgeRangeMaxMonths,
                Images = product.Images
                    .OrderBy(i => i.DisplayOrder)
                    .Select(i => new ProductImageDto { Url = i.ImageUrl, IsPrimary = i.IsPrimary })
                    .ToList(),
                Variants = product.Variants
                    .Select(v => new ProductVariantDto
                    {
                        Id = v.Id,
                        Sku = v.Sku,
                        Size = v.Size,
                        Color = v.Color,
                        Price = v.Price,
                        AvailableQuantity = v.AvailableQuantity
                    })
                    .ToList()
            };
        }
    }
}
