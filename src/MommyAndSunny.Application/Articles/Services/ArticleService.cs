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

        public async Task<List<ArticleCommentDto>> GetCommentsAsync(Guid articleId, CancellationToken cancellationToken = default)
        {
            return await _context.ArticleComments
                .Include(c => c.User)
                .Where(c => c.ArticleId == articleId)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new ArticleCommentDto
                {
                    Id = c.Id,
                    UserFullName = c.User.FullName,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<ArticleCommentDto> CreateCommentAsync(
            Guid articleId, Guid userId, CreateArticleCommentRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Content))
            {
                throw new DomainException("Nội dung bình luận không được để trống");
            }

            var articleExists = await _context.Articles
                .AnyAsync(a => a.Id == articleId && a.Status == ArticleStatus.Published, cancellationToken);
            if (!articleExists)
            {
                throw new NotFoundException("bài viết", articleId.ToString());
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
                ?? throw new UnauthorizedException("Yêu cầu đăng nhập");

            var comment = new Domain.Entities.ArticleComment
            {
                ArticleId = articleId,
                UserId = userId,
                Content = request.Content.Trim()
            };

            _context.ArticleComments.Add(comment);
            await _context.SaveChangesAsync(cancellationToken);

            return new ArticleCommentDto
            {
                Id = comment.Id,
                UserFullName = user.FullName,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt
            };
        }
    }
}
