using MommyAndSunny.Application.Products.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Products.Services
{
    public interface IReviewService
    {
        Task<ProductReviewsDto> GetReviewsAsync(Guid productId, CancellationToken cancellationToken = default);
        Task<bool> CanUserReviewAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default);
        Task<ReviewDto> CreateReviewAsync(
            Guid userId, Guid productId, CreateReviewRequest request, CancellationToken cancellationToken = default);
    }
}
