using Microsoft.EntityFrameworkCore;
using MommyAndSunny.Application.Articles.DTOs;
using MommyAndSunny.Application.Common.Interfaces;
using MommyAndSunny.Application.Common.Models;
using MommyAndSunny.Domain.Enums;
using MommyAndSunny.Domain.Exceptions;

namespace MommyAndSunny.Application.Articles.Services
{
    public class ArticleService : IArticleService
    {
        private readonly IApplicationDbContext _context;

        public ArticleService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<ArticleListItemDto>> GetArticlesAsync(ArticleFilterRequest filter, CancellationToken cancellationToken = default)
        {
            // Chỉ trả bài Published — API công khai không bao giờ lộ bài Draft ra ngoài
            var query = _context.Articles
                .Where(a => a.Status == ArticleStatus.Published)
                .AsQueryable();

            if (filter.Category.HasValue)
            {
                query = query.Where(a => a.Category == filter.Category.Value);
            }

            query = query.OrderByDescending(a => a.PublishedAt);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(a => new ArticleListItemDto
                {
                    Id = a.Id,
                    Title = a.Title,
                    Slug = a.Slug,
                    Excerpt = a.Excerpt,
                    CoverImageUrl = a.CoverImageUrl,
                    AuthorName = a.AuthorName,
                    Category = a.Category.ToString(),
                    PublishedAt = a.PublishedAt
                })
                .ToListAsync(cancellationToken);

            return new PagedResult<ArticleListItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }

        public async Task<ArticleDto> GetArticleBySlugAsync(string slug, CancellationToken cancellationToken = default)
        {
            var article = await _context.Articles
                .FirstOrDefaultAsync(a => a.Slug == slug && a.Status == ArticleStatus.Published, cancellationToken);

            if (article is null)
            {
                throw new NotFoundException("bài viết", slug);
            }

            return new ArticleDto
            {
                Id = article.Id,
                Title = article.Title,
                Slug = article.Slug,
                Excerpt = article.Excerpt,
                Content = article.Content,
                CoverImageUrl = article.CoverImageUrl,
                AuthorName = article.AuthorName,
                Category = article.Category.ToString(),
                PublishedAt = article.PublishedAt
            };
        }
    }
}
