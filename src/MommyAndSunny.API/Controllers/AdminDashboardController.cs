using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MommyAndSunny.Application.Admin.Dashboard.Services;

namespace MommyAndSunny.API.Controllers
{
    [ApiController]
    [Route("api/admin/dashboard")]
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public AdminDashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
        {
            var result = await _dashboardService.GetSummaryAsync(cancellationToken);
            return Ok(new { success = true, data = result, message = (string?)null });
        }

        [HttpGet("revenue-chart")]
        public async Task<IActionResult> GetRevenueChart([FromQuery] int days, CancellationToken cancellationToken)
        {
            var result = await _dashboardService.GetRevenueChartAsync(days == 0 ? 7 : days, cancellationToken);
            return Ok(new { success = true, data = result, message = (string?)null });
        }
    }
}
