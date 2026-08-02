using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MommyAndSunny.Application.Admin.Reviews.DTOs;
using MommyAndSunny.Application.Admin.Reviews.Services;

namespace MommyAndSunny.API.Controllers
{
    [ApiController]
    [Route("api/admin/reviews")]
    [Authorize(Roles = "Admin")]
    public class AdminReviewsController : ControllerBase
    {
        private readonly IAdminReviewService _service;

        public AdminReviewsController(IAdminReviewService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetReviews(
            [FromQuery] AdminReviewFilterRequest filter, CancellationToken cancellationToken)
        {
            var result = await _service.GetReviewsAsync(filter, cancellationToken);
            return Ok(new { success = true, data = result, message = (string?)null });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReview(Guid id, CancellationToken cancellationToken)
        {
            await _service.DeleteReviewAsync(id, cancellationToken);
            return NoContent();
        }
    }

}
