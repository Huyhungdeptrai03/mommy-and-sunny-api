using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MommyAndSunny.Application.Admin.Products.DTOs;
using MommyAndSunny.Application.Admin.Products.Services;

namespace MommyAndSunny.API.Controllers
{
    [ApiController]
    [Route("api/admin/products")]
    [Authorize(Roles = "Admin")]
    public class AdminProductsController : ControllerBase
    {
        private readonly IAdminProductService _adminProductService;

        public AdminProductsController(IAdminProductService adminProductService)
        {
            _adminProductService = adminProductService;
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts([FromQuery] AdminProductFilterRequest filter, CancellationToken cancellationToken)
        {
            var result = await _adminProductService.GetProductsAsync(filter, cancellationToken);
            return Ok(new { success = true, data = result, message = (string?)null });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _adminProductService.GetProductByIdAsync(id, cancellationToken);
            return Ok(new { success = true, data = result, message = (string?)null });
        }

        [HttpPost]
        public async Task<IActionResult> CreateProduct(CreateProductRequest request, CancellationToken cancellationToken)
        {
            var result = await _adminProductService.CreateProductAsync(request, cancellationToken);
            return StatusCode(201, new { success = true, data = result, message = (string?)null });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(Guid id, UpdateProductRequest request, CancellationToken cancellationToken)
        {
            var result = await _adminProductService.UpdateProductAsync(id, request, cancellationToken);
            return Ok(new { success = true, data = result, message = (string?)null });
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateProductStatus(Guid id, UpdateProductStatusRequest request, CancellationToken cancellationToken)
        {
            await _adminProductService.UpdateProductStatusAsync(id, request, cancellationToken);
            return Ok(new { success = true, data = (object?)null, message = "Cập nhật trạng thái thành công" });
        }

        [HttpPost("{id}/images")]
        public async Task<IActionResult> AddImage(Guid id, CreateProductImageRequest request, CancellationToken cancellationToken)
        {
            var result = await _adminProductService.AddImageAsync(id, request, cancellationToken);
            return StatusCode(201, new { success = true, data = result, message = (string?)null });
        }

        [HttpDelete("{id}/images/{imageId}")]
        public async Task<IActionResult> DeleteImage(Guid id, Guid imageId, CancellationToken cancellationToken)
        {
            await _adminProductService.DeleteImageAsync(id, imageId, cancellationToken);
            return NoContent();
        }

        [HttpPost("{id}/specifications")]
        public async Task<IActionResult> AddSpecification(Guid id, CreateProductSpecificationRequest request, CancellationToken cancellationToken)
        {
            var result = await _adminProductService.AddSpecificationAsync(id, request, cancellationToken);
            return StatusCode(201, new { success = true, data = result, message = (string?)null });
        }

        [HttpDelete("{id}/specifications/{specificationId}")]
        public async Task<IActionResult> DeleteSpecification(Guid id, Guid specificationId, CancellationToken cancellationToken)
        {
            await _adminProductService.DeleteSpecificationAsync(id, specificationId, cancellationToken);
            return NoContent();
        }

        [HttpPost("{id}/variants")]
        public async Task<IActionResult> AddVariant(Guid id, CreateProductVariantRequest request, CancellationToken cancellationToken)
        {
            var result = await _adminProductService.AddVariantAsync(id, request, cancellationToken);
            return StatusCode(201, new { success = true, data = result, message = (string?)null });
        }
    }
}
