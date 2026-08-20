using Microsoft.EntityFrameworkCore;
using MommyAndSunny.Application.Admin.Articles.DTOs;
using MommyAndSunny.Application.Common.Interfaces;
using MommyAndSunny.Application.Common.Models;
using MommyAndSunny.Application.Common.Utils;
using MommyAndSunny.Domain.Entities;
using MommyAndSunny.Domain.Enums;
using MommyAndSunny.Domain.Exceptions;

namespace MommyAndSunny.Application.Admin.Articles.Services
{
    public class AdminArticleService : IAdminArticleService
    {
        private readonly IApplicationDbContext _context;

        public AdminArticleService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<AdminArticleListItemDto>> GetArticlesAsync(AdminArticleFilterRequest filter, CancellationToken cancellationToken = default)
        {
            // Khác API công khai: KHÔNG lọc theo Status mặc định — admin cần thấy cả bài Nháp
            var query = _context.Articles.AsQueryable();

            if (filter.Status.HasValue)
            {
                query = query.Where(a => a.Status == filter.Status.Value);
            }

            if (filter.Category.HasValue)
            {
                query = query.Where(a => a.Category == filter.Category.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.ToLower();
                query = query.Where(a => a.Title.ToLower().Contains(term));
            }

            query = query.OrderByDescending(a => a.CreatedAt);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(a => new AdminArticleListItemDto
                {
                    Id = a.Id,
                    Title = a.Title,
                    Slug = a.Slug,
                    Category = a.Category.ToString(),
                    Status = a.Status.ToString(),
                    CoverImageUrl = a.CoverImageUrl,
                    AuthorName = a.AuthorName,
                    PublishedAt = a.PublishedAt,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync(cancellationToken);

            return new PagedResult<AdminArticleListItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }

        public async Task<AdminArticleDto> GetArticleByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var article = await _context.Articles.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
            if (article is null)
            {
                throw new NotFoundException("bài viết", id);
            }

            return MapToDto(article);
        }

        public async Task<AdminArticleDto> CreateArticleAsync(CreateArticleRequest request, CancellationToken cancellationToken = default)
        {
            var article = new Article
            {
                Title = request.Title,
                Slug = await GenerateUniqueSlugAsync(request.Title, cancellationToken),
                Excerpt = request.Excerpt,
                Content = request.Content,
                CoverImageUrl = request.CoverImageUrl,
                AuthorName = request.AuthorName,
                Category = request.Category,
                // Luôn tạo ở Draft — admin tự bấm "Xuất bản" sau khi ưng ý nội dung
                Status = ArticleStatus.Draft
            };

            _context.Articles.Add(article);
            await _context.SaveChangesAsync(cancellationToken);

            return MapToDto(article);
        }

        public async Task<AdminArticleDto> UpdateArticleAsync(Guid id, UpdateArticleRequest request, CancellationToken cancellationToken = default)
        {
            var article = await _context.Articles.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
            if (article is null)
            {
                throw new NotFoundException("bài viết", id);
            }

            article.Title = request.Title;
            article.Excerpt = request.Excerpt;
            article.Content = request.Content;
            article.CoverImageUrl = request.CoverImageUrl;
            article.AuthorName = request.AuthorName;
            article.Category = request.Category;
            // Cố ý KHÔNG đổi Slug khi update — tránh vỡ link cũ đã được chia sẻ/Google index

            await _context.SaveChangesAsync(cancellationToken);

            return MapToDto(article);
        }

        public async Task UpdateArticleStatusAsync(Guid id, UpdateArticleStatusRequest request, CancellationToken cancellationToken = default)
        {
            var article = await _context.Articles.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
            if (article is null)
            {
                throw new NotFoundException("bài viết", id);
            }

            // Chỉ ghi PublishedAt lần đầu chuyển sang Published — nếu sau đó ẩn rồi
            // xuất bản lại thì giữ nguyên ngày đăng gốc, không tính là bài mới
            if (request.Status == ArticleStatus.Published && article.PublishedAt is null)
            {
                article.PublishedAt = DateTime.UtcNow;
            }

            article.Status = request.Status;
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteArticleAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var article = await _context.Articles.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
            if (article is null)
            {
                throw new NotFoundException("bài viết", id);
            }

            _context.Articles.Remove(article);
            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task<string> GenerateUniqueSlugAsync(string title, CancellationToken cancellationToken)
        {
            var baseSlug = SlugGenerator.GenerateSlug(title);
            var slug = baseSlug;
            var counter = 1;

            while (await _context.Articles.AnyAsync(a => a.Slug == slug, cancellationToken))
            {
                counter++;
                slug = $"{baseSlug}-{counter}";
            }

            return slug;
        }

        private static AdminArticleDto MapToDto(Article article)
        {
            return new AdminArticleDto
            {
                Id = article.Id,
                Title = article.Title,
                Slug = article.Slug,
                Excerpt = article.Excerpt,
                Content = article.Content,
                CoverImageUrl = article.CoverImageUrl,
                AuthorName = article.AuthorName,
                Category = article.Category.ToString(),
                Status = article.Status.ToString(),
                PublishedAt = article.PublishedAt,
                CreatedAt = article.CreatedAt,
                UpdatedAt = article.UpdatedAt
            };
        }
    }
}
