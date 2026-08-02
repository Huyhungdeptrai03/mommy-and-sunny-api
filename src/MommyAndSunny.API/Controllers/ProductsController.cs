using Microsoft.AspNetCore.Mvc;
using MommyAndSunny.Application.Products.DTOs;
using MommyAndSunny.Application.Products.Services;

namespace MommyAndSunny.API.Controllers
{
    [ApiController]
    [Route("api/products")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts([FromQuery] ProductFilterRequest filter, CancellationToken cancellationToken)
        {
            var result = await _productService.GetProductsAsync(filter, cancellationToken);
            return Ok(new { success = true, data = result, message = (string?)null });
        }

        [HttpGet("{slug}")]
        public async Task<IActionResult> GetProductBySlug(string slug, CancellationToken cancellationToken)
        {
            var result = await _productService.GetProductBySlugAsync(slug, cancellationToken);
            return Ok(new { success = true, data = result, message = (string?)null });
        }

        [HttpGet("flash-sale")]
        public async Task<IActionResult> GetFlashSale(CancellationToken cancellationToken)
        {
            var result = await _productService.GetFlashSaleAsync(cancellationToken);
            return Ok(new { success = true, data = result, message = (string?)null });
        }
    }
}
