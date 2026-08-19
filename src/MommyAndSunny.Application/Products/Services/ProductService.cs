using Microsoft.EntityFrameworkCore;
using MommyAndSunny.Application.Common.Interfaces;
using MommyAndSunny.Application.Common.Models;
using MommyAndSunny.Application.Products.DTOs;
using MommyAndSunny.Domain.Enums;
using MommyAndSunny.Domain.Exceptions;

namespace MommyAndSunny.Application.Products.Services;

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

        if (filter.PurchaseBatchId.HasValue)
        {
            query = query.Where(p => p.PurchaseBatchId == filter.PurchaseBatchId.Value);
        }

        if (filter.AgeMonths.HasValue)
        {
            // Sản phẩm không gắn tuổi (đồ gia dụng, mỹ phẩm...) sẽ KHÔNG xuất hiện khi
            // khách lọc theo tuổi — viết rõ điều kiện != null thay vì phụ thuộc ngầm định
            // vào cách SQL tự loại bỏ so sánh với NULL
            query = query.Where(p =>
                p.AgeRangeMinMonths != null && p.AgeRangeMaxMonths != null &&
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
            // Khớp cả Tên, Thương hiệu VÀ Danh mục (không phân biệt hoa/thường qua ToLower) —
            // để tìm "nước hoa" ra đúng nhóm sản phẩm dù tên sản phẩm không chứa chữ đó,
            // thay vì chỉ khớp Name như trước (MVP ban đầu). Dùng ToLower thay vì EF.Functions.ILike
            // (đặc thù Npgsql) để Application layer không phụ thuộc ngược vào Infrastructure.
            // Full-text nâng cao để Tier 2
            var term = filter.Search.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(term) ||
                (p.Brand != null && p.Brand.ToLower().Contains(term)) ||
                p.Category.Name.ToLower().Contains(term));
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
                PurchaseBatchName = p.PurchaseBatch != null ? p.PurchaseBatch.Name : null,
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
            .Include(p => p.Specifications)
            .Include(p => p.Variants)
            .Include(p => p.PurchaseBatch)
            .FirstOrDefaultAsync(p => p.Slug == slug && p.Status == ProductStatus.Published, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException("sản phẩm", slug);
        }

        var reviewStats = await _context.Reviews
            .Where(r => r.ProductId == product.Id)
            .GroupBy(r => 1)
            .Select(g => new { Average = g.Average(r => r.Rating), Count = g.Count() })
            .FirstOrDefaultAsync(cancellationToken);

        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Slug = product.Slug,
            Description = product.Description,
            Brand = product.Brand,
            AgeRangeMinMonths = product.AgeRangeMinMonths,
            AgeRangeMaxMonths = product.AgeRangeMaxMonths,
            AverageRating = reviewStats != null ? Math.Round(reviewStats.Average, 1) : (double?)null,
            ReviewCount = reviewStats?.Count ?? 0,
            PurchaseBatchName = product.PurchaseBatch?.Name,
            EstimatedArrivalDate = product.PurchaseBatch?.EstimatedArrivalDate,
            Images = product.Images
                .OrderBy(i => i.DisplayOrder)
                .Select(i => new ProductImageDto { Url = i.ImageUrl, IsPrimary = i.IsPrimary })
                .ToList(),
            Specifications = product.Specifications
                .OrderBy(s => s.DisplayOrder)
                .Select(s => new ProductSpecificationDto { Label = s.Label, Value = s.Value })
                .ToList(),
            Variants = product.Variants
                .Select(v => new ProductVariantDto
                {
                    Id = v.Id,
                    Sku = v.Sku,
                    Size = v.Size,
                    Color = v.Color,
                    Price = v.Price,
                    CompareAtPrice = v.CompareAtPrice,
                    SaleEndsAt = v.SaleEndsAt,
                    AvailableQuantity = v.AvailableQuantity
                })
                .ToList()
        };
    }

    public async Task<List<FlashSaleItemDto>> GetFlashSaleAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        // Điều kiện "đang Flash Sale": có CompareAtPrice > Price VÀ SaleEndsAt còn ở tương lai
        var rawVariants = await _context.ProductVariants
            .Include(v => v.Product)
                .ThenInclude(p => p.Images)
            .Where(v =>
                v.CompareAtPrice != null && v.CompareAtPrice > v.Price &&
                v.SaleEndsAt != null && v.SaleEndsAt > now &&
                v.Product.Status == ProductStatus.Published)
            .OrderBy(v => v.SaleEndsAt)
            .ToListAsync(cancellationToken);

        // Tính % giảm giá ở C# (không dịch sang SQL) — tránh rủi ro EF Core
        // không dịch được phép tính Math.Round trên 1 số hệ CSDL
        return rawVariants.Select(v => new FlashSaleItemDto
        {
            ProductId = v.ProductId,
            ProductName = v.Product.Name,
            ProductSlug = v.Product.Slug,
            ImageUrl = v.Product.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl,
            VariantId = v.Id,
            Price = v.Price,
            CompareAtPrice = v.CompareAtPrice!.Value,
            DiscountPercent = (int)Math.Round((1 - v.Price / v.CompareAtPrice!.Value) * 100),
            SaleEndsAt = v.SaleEndsAt!.Value,
            AvailableQuantity = v.AvailableQuantity
        }).ToList();
    }

    public async Task<List<string>> GetBrandsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Where(p => p.Status == ProductStatus.Published && p.Brand != null && p.Brand != "")
            .Select(p => p.Brand!)
            .Distinct()
            .OrderBy(b => b)
            .Take(12)
            .ToListAsync(cancellationToken);
    }
}


