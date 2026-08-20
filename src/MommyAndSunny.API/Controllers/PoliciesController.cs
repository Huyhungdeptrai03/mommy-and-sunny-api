using Microsoft.AspNetCore.Mvc;
using MommyAndSunny.Application.Policies.Services;

namespace MommyAndSunny.API.Controllers
{
    [ApiController]
    [Route("api/policies")]
    public class PoliciesController : ControllerBase
    {
        private readonly IPolicyService _policyService;

        public PoliciesController(IPolicyService policyService)
        {
            _policyService = policyService;
        }

        [HttpGet("{slug}")]
        public async Task<IActionResult> GetBySlug(string slug, CancellationToken cancellationToken)
        {
            var result = await _policyService.GetBySlugAsync(slug, cancellationToken);
            return Ok(new { success = true, data = result, message = (string?)null });
        }
    }
}
