using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MommyAndSunny.Application.Common.Interfaces;
using MommyAndSunny.Application.Products.DTOs;
using MommyAndSunny.Application.Products.Services;
using MommyAndSunny.Domain.Exceptions;

namespace MommyAndSunny.API.Controllers
{
    [ApiController]
    [Route("api/products/{productId}/reviews")]
    public class ReviewsController : ControllerBase
    {
        private readonly IReviewService _reviewService;
        private readonly ICurrentUserService _currentUserService;

        public ReviewsController(IReviewService reviewService, ICurrentUserService currentUserService)
        {
            _reviewService = reviewService;
            _currentUserService = currentUserService;
        }

        [HttpGet]
        public async Task<IActionResult> GetReviews(Guid productId, CancellationToken cancellationToken)
        {
            var result = await _reviewService.GetReviewsAsync(productId, cancellationToken);
            return Ok(new { success = true, data = result, message = (string?)null });
        }

        [HttpGet("can-review")]
        [Authorize]
        public async Task<IActionResult> CanReview(Guid productId, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId ?? throw new UnauthorizedException("Yêu cầu đăng nhập");
            var canReview = await _reviewService.CanUserReviewAsync(userId, productId, cancellationToken);
            return Ok(new { success = true, data = new { canReview }, message = (string?)null });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateReview(
            Guid productId, CreateReviewRequest request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId ?? throw new UnauthorizedException("Yêu cầu đăng nhập");
            var result = await _reviewService.CreateReviewAsync(userId, productId, request, cancellationToken);
            return StatusCode(201, new { success = true, data = result, message = "Cảm ơn bạn đã đánh giá!" });
        }
    }
}
