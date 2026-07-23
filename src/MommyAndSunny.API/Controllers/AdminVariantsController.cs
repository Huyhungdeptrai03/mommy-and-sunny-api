using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MommyAndSunny.Application.Admin.Products.DTOs;
using MommyAndSunny.Application.Admin.Products.Services;

namespace MommyAndSunny.API.Controllers
{
    [ApiController]
    [Route("api/admin/variants")]
    [Authorize(Roles = "Admin")]
    public class AdminVariantsController : ControllerBase
    {
        private readonly IAdminProductService _adminProductService;

        public AdminVariantsController(IAdminProductService adminProductService)
        {
            _adminProductService = adminProductService;
        }

        [HttpPut("{variantId}")]
        public async Task<IActionResult> UpdateVariant(Guid variantId, UpdateProductVariantRequest request, CancellationToken cancellationToken)
        {
            var result = await _adminProductService.UpdateVariantAsync(variantId, request, cancellationToken);
            return Ok(new { success = true, data = result, message = (string?)null });
        }

        [HttpDelete("{variantId}")]
        public async Task<IActionResult> DeleteVariant(Guid variantId, CancellationToken cancellationToken)
        {
            await _adminProductService.DeleteVariantAsync(variantId, cancellationToken);
            return NoContent();
        }
    }
}
