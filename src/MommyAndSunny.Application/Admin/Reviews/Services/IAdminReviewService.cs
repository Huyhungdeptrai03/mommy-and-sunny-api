using MommyAndSunny.Application.Admin.Reviews.DTOs;
using MommyAndSunny.Application.Common.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Admin.Reviews.Services
{
    public interface IAdminReviewService
    {
        Task<PagedResult<AdminReviewDto>> GetReviewsAsync(
            AdminReviewFilterRequest filter, CancellationToken cancellationToken = default);

        Task DeleteReviewAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
