using MommyAndSunny.Application.Categories.DTOs;
using MommyAndSunny.Application.Common.Interfaces;
using MommyAndSunny.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Categories.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly IApplicationDbContext _context;

        public CategoryService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<CategoryDto>> GetCategoryTreeAsync(CancellationToken cancellationToken = default)
        {
            // Lấy toàn bộ danh mục (số lượng nhỏ, không cần phân trang) rồi dựng cây trong memory —
            // đơn giản và đủ nhanh cho quy mô danh mục của 1 website bán hàng thông thường
            var allCategories = await _context.Categories
                .Where(c => c.IsActive)
                .ToListAsync(cancellationToken);

            return BuildTree(allCategories, parentId: null);
        }

        private static List<CategoryDto> BuildTree(List<Category> allCategories, Guid? parentId)
        {
            return allCategories
                .Where(c => c.ParentCategoryId == parentId)
                .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    ImageUrl = c.ImageUrl,
                    Children = BuildTree(allCategories, c.Id)
                })
                .ToList();
        }
    }
}
