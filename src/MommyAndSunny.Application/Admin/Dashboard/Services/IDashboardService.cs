using MommyAndSunny.Application.Admin.Dashboard.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Admin.Dashboard.Services
{
    public interface IDashboardService
    {
        Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
        Task<List<RevenueChartPointDto>> GetRevenueChartAsync(DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);
    }
}
