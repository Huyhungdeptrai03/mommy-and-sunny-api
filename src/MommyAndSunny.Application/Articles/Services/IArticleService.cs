using MommyAndSunny.Application.Articles.DTOs;
using MommyAndSunny.Application.Common.Models;

namespace MommyAndSunny.Application.Articles.Services
{
    public interface IArticleService
    {
        Task<PagedResult<ArticleListItemDto>> GetArticlesAsync(ArticleFilterRequest filter, CancellationToken cancellationToken = default);
        Task<ArticleDto> GetArticleBySlugAsync(string slug, CancellationToken cancellationToken = default);
        Task<List<ArticleCommentDto>> GetCommentsAsync(Guid articleId, CancellationToken cancellationToken = default);
        Task<ArticleCommentDto> CreateCommentAsync(Guid articleId, Guid userId, CreateArticleCommentRequest request, CancellationToken cancellationToken = default);
    }
}
