using Microsoft.AspNetCore.Mvc;
using MommyAndSunny.Application.PublicStats.Services;

namespace MommyAndSunny.API.Controllers
{
    [ApiController]
    [Route("api/stats")]
    public class PublicStatsController : ControllerBase
    {
        private readonly IPublicStatsService _service;

        public PublicStatsController(IPublicStatsService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
        {
            var result = await _service.GetStatsAsync(cancellationToken);
            return Ok(new { success = true, data = result, message = (string?)null });
        }
    }
}
