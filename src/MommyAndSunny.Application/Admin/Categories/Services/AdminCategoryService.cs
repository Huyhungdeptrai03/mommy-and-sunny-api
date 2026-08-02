using Microsoft.EntityFrameworkCore;
using MommyAndSunny.Application.Admin.Categories.DTOs;
using MommyAndSunny.Application.Common.Interfaces;
using MommyAndSunny.Application.Common.Utils;
using MommyAndSunny.Domain.Entities;
using MommyAndSunny.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Admin.Categories.Services
{
    public class AdminCategoryService : IAdminCategoryService
    {
        private readonly IApplicationDbContext _context;

        public AdminCategoryService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<AdminCategoryDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var categories = await _context.Categories
                .OrderBy(c => c.Name)
                .ToListAsync(cancellationToken);

            var productCounts = await _context.Products
                .GroupBy(p => p.CategoryId)
                .Select(g => new { CategoryId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.CategoryId, g => g.Count, cancellationToken);

            return categories.Select(c => new AdminCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                ParentCategoryId = c.ParentCategoryId,
                ParentCategoryName = c.ParentCategoryId != null
                    ? categories.FirstOrDefault(p => p.Id == c.ParentCategoryId)?.Name
                    : null,
                ImageUrl = c.ImageUrl,
                ProductCount = productCounts.GetValueOrDefault(c.Id, 0)
            }).ToList();
        }

        public async Task<AdminCategoryDto> CreateAsync(
            CreateCategoryRequest request, CancellationToken cancellationToken = default)
        {
            if (request.ParentCategoryId.HasValue)
            {
                var parentExists = await _context.Categories
                    .AnyAsync(c => c.Id == request.ParentCategoryId, cancellationToken);
                if (!parentExists)
                {
                    throw new NotFoundException("danh mục cha", request.ParentCategoryId.Value.ToString());
                }
            }

            var slug = SlugGenerator.GenerateSlug(request.Name);
            var slugExists = await _context.Categories.AnyAsync(c => c.Slug == slug, cancellationToken);
            if (slugExists)
            {
                slug = $"{slug}-{Guid.NewGuid().ToString()[..6]}";
            }

            var category = new Category
            {
                Name = request.Name,
                Slug = slug,
                ParentCategoryId = request.ParentCategoryId,
                ImageUrl = request.ImageUrl
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync(cancellationToken);

            return new AdminCategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Slug = category.Slug,
                ParentCategoryId = category.ParentCategoryId,
                ImageUrl = category.ImageUrl,
                ProductCount = 0
            };
        }

        public async Task<AdminCategoryDto> UpdateAsync(
            Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken = default)
        {
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
                ?? throw new NotFoundException("danh mục", id.ToString());

            if (request.ParentCategoryId == id)
            {
                throw new DomainException("Danh mục không thể là danh mục cha của chính nó");
            }

            if (request.ParentCategoryId.HasValue)
            {
                var parentExists = await _context.Categories
                    .AnyAsync(c => c.Id == request.ParentCategoryId, cancellationToken);
                if (!parentExists)
                {
                    throw new NotFoundException("danh mục cha", request.ParentCategoryId.Value.ToString());
                }
            }

            category.Name = request.Name;
            category.ParentCategoryId = request.ParentCategoryId;
            category.ImageUrl = request.ImageUrl;

            await _context.SaveChangesAsync(cancellationToken);

            return new AdminCategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Slug = category.Slug,
                ParentCategoryId = category.ParentCategoryId,
                ImageUrl = category.ImageUrl,
                ProductCount = await _context.Products.CountAsync(p => p.CategoryId == id, cancellationToken)
            };
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
                ?? throw new NotFoundException("danh mục", id.ToString());

            // Chặn xóa nếu còn sản phẩm hoặc danh mục con đang tham chiếu tới —
            // tránh dữ liệu mồ côi (orphan) hoặc lỗi ràng buộc khóa ngoại ở database
            var hasProducts = await _context.Products.AnyAsync(p => p.CategoryId == id, cancellationToken);
            if (hasProducts)
            {
                throw new ConflictException(
                    "Không thể xóa danh mục đang có sản phẩm. Vui lòng chuyển sản phẩm sang danh mục khác trước.");
            }

            var hasChildren = await _context.Categories.AnyAsync(c => c.ParentCategoryId == id, cancellationToken);
            if (hasChildren)
            {
                throw new ConflictException(
                    "Không thể xóa danh mục đang có danh mục con. Vui lòng xóa/di chuyển danh mục con trước.");
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

}
