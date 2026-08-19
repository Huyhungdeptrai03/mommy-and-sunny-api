using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MommyAndSunny.Application.Wishlist.Services;

namespace MommyAndSunny.API.Controllers
{
    [ApiController]
    [Route("api/wishlist")]
    [Authorize]
    public class WishlistController : ControllerBase
    {
        private readonly IWishlistService _service;

        public WishlistController(IWishlistService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyWishlist(CancellationToken cancellationToken)
        {
            var result = await _service.GetMyWishlistAsync(cancellationToken);
            return Ok(new { success = true, data = result, message = (string?)null });
        }

        [HttpPost("{productId}")]
        public async Task<IActionResult> Add(Guid productId, CancellationToken cancellationToken)
        {
            await _service.AddAsync(productId, cancellationToken);
            return Ok(new { success = true, data = (object?)null, message = "Đã thêm vào yêu thích" });
        }

        [HttpDelete("{productId}")]
        public async Task<IActionResult> Remove(Guid productId, CancellationToken cancellationToken)
        {
            await _service.RemoveAsync(productId, cancellationToken);
            return Ok(new { success = true, data = (object?)null, message = "Đã gỡ khỏi yêu thích" });
        }
    }
}
