using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MommyAndSunny.Application.Cart.DTOs;
using MommyAndSunny.Application.Cart.Services;

namespace MommyAndSunny.API.Controllers
{
    [ApiController]
    [Route("api/cart")]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        [HttpGet]
        public async Task<IActionResult> GetCart(CancellationToken cancellationToken)
        {
            var result = await _cartService.GetCartAsync(cancellationToken);
            return Ok(new { success = true, data = result, message = (string?)null });
        }

        [HttpPost("items")]
        public async Task<IActionResult> AddItem(AddCartItemRequest request, CancellationToken cancellationToken)
        {
            var result = await _cartService.AddItemAsync(request, cancellationToken);
            return StatusCode(201, new { success = true, data = result, message = (string?)null });
        }

        [HttpPut("items/{itemId}")]
        public async Task<IActionResult> UpdateItem(Guid itemId, UpdateCartItemRequest request, CancellationToken cancellationToken)
        {
            var result = await _cartService.UpdateItemAsync(itemId, request, cancellationToken);
            return Ok(new { success = true, data = result, message = (string?)null });
        }

        [HttpDelete("items/{itemId}")]
        public async Task<IActionResult> RemoveItem(Guid itemId, CancellationToken cancellationToken)
        {
            await _cartService.RemoveItemAsync(itemId, cancellationToken);
            return NoContent();
        }
    }
}
