using MommyAndSunny.Application.Admin.Stats.DTOs;
using MommyAndSunny.Application.Common.Models;

namespace MommyAndSunny.Application.Admin.Stats.Services
{
    public interface IAdminStatsService
    {
        Task<List<MonthlyStatsDto>> GetMonthlyStatsAsync(int months, CancellationToken cancellationToken = default);
        Task<PagedResult<CustomerStatsItemDto>> GetCustomerStatsAsync(CustomerStatsFilterRequest filter, CancellationToken cancellationToken = default);
    }
}
