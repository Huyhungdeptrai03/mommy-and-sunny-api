using MommyAndSunny.Application.Admin.Articles.DTOs;
using MommyAndSunny.Application.Common.Models;

namespace MommyAndSunny.Application.Admin.Articles.Services
{
    public interface IAdminArticleService
    {
        Task<PagedResult<AdminArticleListItemDto>> GetArticlesAsync(AdminArticleFilterRequest filter, CancellationToken cancellationToken = default);
        Task<AdminArticleDto> GetArticleByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<AdminArticleDto> CreateArticleAsync(CreateArticleRequest request, CancellationToken cancellationToken = default);
        Task<AdminArticleDto> UpdateArticleAsync(Guid id, UpdateArticleRequest request, CancellationToken cancellationToken = default);
        Task UpdateArticleStatusAsync(Guid id, UpdateArticleStatusRequest request, CancellationToken cancellationToken = default);
        Task DeleteArticleAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
