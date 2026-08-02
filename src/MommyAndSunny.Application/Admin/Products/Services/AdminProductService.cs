using MommyAndSunny.Application.Admin.Products.DTOs;
using MommyAndSunny.Application.Common.Interfaces;
using MommyAndSunny.Application.Common.Models;
using MommyAndSunny.Application.Common.Utils;
using MommyAndSunny.Domain.Entities;
using MommyAndSunny.Domain.Enums;
using MommyAndSunny.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Admin.Products.Services
{
    public class AdminProductService : IAdminProductService
    {
        private readonly IApplicationDbContext _context;

        public AdminProductService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<AdminProductListItemDto>> GetProductsAsync(
            AdminProductFilterRequest filter,
            CancellationToken cancellationToken = default)
        {
            // Khác API công khai: KHÔNG lọc theo Status mặc định — admin cần thấy cả Draft/Discontinued
            var query = _context.Products.Include(p => p.Category).AsQueryable();

            if (filter.Status.HasValue)
            {
                query = query.Where(p => p.Status == filter.Status.Value);
            }

            if (filter.CategoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == filter.CategoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                query = query.Where(p => p.Name.Contains(filter.Search));
            }

            query = query.OrderByDescending(p => p.CreatedAt);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(p => new AdminProductListItemDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Slug = p.Slug,
                    Status = p.Status.ToString(),
                    Brand = p.Brand,
                    CategoryName = p.Category.Name,
                    VariantCount = p.Variants.Count,
                    TotalStock = p.Variants.Sum(v => v.StockQuantity),
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync(cancellationToken);

            return new PagedResult<AdminProductListItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }

        public async Task<AdminProductDetailDto> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

            if (product is null)
            {
                throw new NotFoundException("sản phẩm", id);
            }

            return MapToDetailDto(product);
        }

        public async Task<AdminProductDetailDto> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
        {
            var categoryExists = await _context.Categories.AnyAsync(c => c.Id == request.CategoryId, cancellationToken);
            if (!categoryExists)
            {
                throw new NotFoundException("danh mục", request.CategoryId);
            }

            var product = new Product
            {
                CategoryId = request.CategoryId,
                Name = request.Name,
                Slug = await GenerateUniqueSlugAsync(request.Name, cancellationToken),
                Description = request.Description,
                Brand = request.Brand,
                AgeRangeMinMonths = request.AgeRangeMinMonths,
                AgeRangeMaxMonths = request.AgeRangeMaxMonths,
                // Luôn tạo ở Draft — admin tự bấm xuất bản sau khi thêm đủ ảnh + biến thể
                Status = ProductStatus.Draft
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync(cancellationToken);

            return MapToDetailDto(product);
        }

        public async Task<AdminProductDetailDto> UpdateProductAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

            if (product is null)
            {
                throw new NotFoundException("sản phẩm", id);
            }

            var categoryExists = await _context.Categories.AnyAsync(c => c.Id == request.CategoryId, cancellationToken);
            if (!categoryExists)
            {
                throw new NotFoundException("danh mục", request.CategoryId);
            }

            product.CategoryId = request.CategoryId;
            product.Name = request.Name;
            product.Description = request.Description;
            product.Brand = request.Brand;
            product.AgeRangeMinMonths = request.AgeRangeMinMonths;
            product.AgeRangeMaxMonths = request.AgeRangeMaxMonths;
            // Cố ý KHÔNG đổi Slug khi update — tránh vỡ link cũ đã được khách lưu/Google index

            await _context.SaveChangesAsync(cancellationToken);

            return MapToDetailDto(product);
        }

        public async Task UpdateProductStatusAsync(Guid id, UpdateProductStatusRequest request, CancellationToken cancellationToken = default)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

            if (product is null)
            {
                throw new NotFoundException("sản phẩm", id);
            }

            if (request.Status == ProductStatus.Published)
            {
                var hasImage = product.Images.Any();
                var hasStockedVariant = product.Variants.Any(v => v.StockQuantity > 0);

                // Đúng điều kiện xuất bản đã thống nhất trong API contract
                if (!hasImage || !hasStockedVariant)
                {
                    throw new DomainException("Cần ít nhất 1 ảnh và 1 biến thể còn hàng trước khi xuất bản");
                }
            }

            product.Status = request.Status;
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<ProductImageAdminDto> AddImageAsync(Guid productId, CreateProductImageRequest request, CancellationToken cancellationToken = default)
        {
            var productExists = await _context.Products.AnyAsync(p => p.Id == productId, cancellationToken);
            if (!productExists)
            {
                throw new NotFoundException("sản phẩm", productId);
            }

            if (request.IsPrimary)
            {
                var currentPrimaryImages = await _context.ProductImages
                    .Where(i => i.ProductId == productId && i.IsPrimary)
                    .ToListAsync(cancellationToken);

                foreach (var img in currentPrimaryImages)
                {
                    img.IsPrimary = false;
                }
            }

            var image = new ProductImage
            {
                ProductId = productId,
                ImageUrl = request.ImageUrl,
                IsPrimary = request.IsPrimary,
                DisplayOrder = request.DisplayOrder
            };

            _context.ProductImages.Add(image);
            await _context.SaveChangesAsync(cancellationToken);

            return new ProductImageAdminDto
            {
                Id = image.Id,
                ImageUrl = image.ImageUrl,
                IsPrimary = image.IsPrimary,
                DisplayOrder = image.DisplayOrder
            };
        }

        public async Task DeleteImageAsync(Guid productId, Guid imageId, CancellationToken cancellationToken = default)
        {
            var image = await _context.ProductImages
                .FirstOrDefaultAsync(i => i.Id == imageId && i.ProductId == productId, cancellationToken);

            if (image is null)
            {
                throw new NotFoundException("ảnh sản phẩm", imageId);
            }

            _context.ProductImages.Remove(image);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<ProductVariantAdminDto> AddVariantAsync(Guid productId, CreateProductVariantRequest request, CancellationToken cancellationToken = default)
        {
            var productExists = await _context.Products.AnyAsync(p => p.Id == productId, cancellationToken);
            if (!productExists)
            {
                throw new NotFoundException("sản phẩm", productId);
            }

            var skuExists = await _context.ProductVariants.AnyAsync(v => v.Sku == request.Sku, cancellationToken);
            if (skuExists)
            {
                throw new ConflictException($"SKU '{request.Sku}' đã tồn tại");
            }

            var variant = new ProductVariant
            {
                ProductId = productId,
                Sku = request.Sku,
                Size = request.Size,
                Color = request.Color,
                Price = request.Price,
                CompareAtPrice = request.CompareAtPrice,
                SaleEndsAt = request.SaleEndsAt,
                StockQuantity = request.StockQuantity,
                ReservedQuantity = 0
            };

            _context.ProductVariants.Add(variant);
            await _context.SaveChangesAsync(cancellationToken);

            return MapToVariantDto(variant);
        }

        public async Task<ProductVariantAdminDto> UpdateVariantAsync(Guid variantId, UpdateProductVariantRequest request, CancellationToken cancellationToken = default)
        {
            var variant = await _context.ProductVariants.FirstOrDefaultAsync(v => v.Id == variantId, cancellationToken);
            if (variant is null)
            {
                throw new NotFoundException("biến thể sản phẩm", variantId);
            }

            if (request.Sku != variant.Sku)
            {
                var skuExists = await _context.ProductVariants
                    .AnyAsync(v => v.Sku == request.Sku && v.Id != variantId, cancellationToken);
                if (skuExists)
                {
                    throw new ConflictException($"SKU '{request.Sku}' đã tồn tại");
                }
            }

            variant.Sku = request.Sku;
            variant.Size = request.Size;
            variant.Color = request.Color;
            variant.Price = request.Price;
            variant.CompareAtPrice = request.CompareAtPrice;
            variant.SaleEndsAt = request.SaleEndsAt;
            variant.StockQuantity = request.StockQuantity;

            await _context.SaveChangesAsync(cancellationToken);

            return MapToVariantDto(variant);
        }

        public async Task DeleteVariantAsync(Guid variantId, CancellationToken cancellationToken = default)
        {
            var variant = await _context.ProductVariants.FirstOrDefaultAsync(v => v.Id == variantId, cancellationToken);
            if (variant is null)
            {
                throw new NotFoundException("biến thể sản phẩm", variantId);
            }

            // Đúng nghiệp vụ đã thống nhất: chỉ xóa được nếu chưa từng xuất hiện trong đơn hàng nào
            var hasOrderHistory = await _context.OrderItems.AnyAsync(i => i.ProductVariantId == variantId, cancellationToken);
            if (hasOrderHistory)
            {
                throw new ConflictException("Không thể xóa biến thể đã từng xuất hiện trong đơn hàng — hãy đặt tồn kho về 0 thay vì xóa");
            }

            _context.ProductVariants.Remove(variant);
            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task<string> GenerateUniqueSlugAsync(string name, CancellationToken cancellationToken)
        {
            var baseSlug = SlugGenerator.GenerateSlug(name);
            var slug = baseSlug;
            var counter = 1;

            while (await _context.Products.AnyAsync(p => p.Slug == slug, cancellationToken))
            {
                counter++;
                slug = $"{baseSlug}-{counter}";
            }

            return slug;
        }

        private static AdminProductDetailDto MapToDetailDto(Product product)
        {
            return new AdminProductDetailDto
            {
                Id = product.Id,
                CategoryId = product.CategoryId,
                Name = product.Name,
                Slug = product.Slug,
                Description = product.Description,
                Brand = product.Brand,
                AgeRangeMinMonths = product.AgeRangeMinMonths,
                AgeRangeMaxMonths = product.AgeRangeMaxMonths,
                Status = product.Status.ToString(),
                Images = product.Images.Select(i => new ProductImageAdminDto
                {
                    Id = i.Id,
                    ImageUrl = i.ImageUrl,
                    IsPrimary = i.IsPrimary,
                    DisplayOrder = i.DisplayOrder
                }).ToList(),
                Variants = product.Variants.Select(MapToVariantDto).ToList()
            };
        }

        private static ProductVariantAdminDto MapToVariantDto(ProductVariant variant)
        {
            return new ProductVariantAdminDto
            {
                Id = variant.Id,
                Sku = variant.Sku,
                Size = variant.Size,
                Color = variant.Color,
                Price = variant.Price,
                CompareAtPrice = variant.CompareAtPrice,
                SaleEndsAt = variant.SaleEndsAt,
                StockQuantity = variant.StockQuantity,
                ReservedQuantity = variant.ReservedQuantity,
                AvailableQuantity = variant.AvailableQuantity
            };
        }
    }
}
