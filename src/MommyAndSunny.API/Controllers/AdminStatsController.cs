using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MommyAndSunny.Application.Admin.Stats.DTOs;
using MommyAndSunny.Application.Admin.Stats.Services;

namespace MommyAndSunny.API.Controllers
{
    [ApiController]
    [Route("api/admin/stats")]
    [Authorize(Roles = "Admin")]
    public class AdminStatsController : ControllerBase
    {
        private readonly IAdminStatsService _adminStatsService;

        public AdminStatsController(IAdminStatsService adminStatsService)
        {
            _adminStatsService = adminStatsService;
        }

        [HttpGet("monthly")]
        public async Task<IActionResult> GetMonthlyStats([FromQuery] int months, CancellationToken cancellationToken)
        {
            var result = await _adminStatsService.GetMonthlyStatsAsync(months, cancellationToken);
            return Ok(new { success = true, data = result, message = (string?)null });
        }

        [HttpGet("customers")]
        public async Task<IActionResult> GetCustomerStats([FromQuery] CustomerStatsFilterRequest filter, CancellationToken cancellationToken)
        {
            var result = await _adminStatsService.GetCustomerStatsAsync(filter, cancellationToken);
            return Ok(new { success = true, data = result, message = (string?)null });
        }
    }
}
