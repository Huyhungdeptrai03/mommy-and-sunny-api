using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MommyAndSunny.Application.Coupons.DTOs;
using MommyAndSunny.Application.Coupons.Services;

namespace MommyAndSunny.API.Controllers
{
    [ApiController]
    [Route("api/admin/coupons")]
    [Authorize(Roles = "Admin")]
    public class AdminCouponsController : ControllerBase
    {
        private readonly IAdminCouponService _service;

        public AdminCouponsController(IAdminCouponService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var result = await _service.GetAllAsync(cancellationToken);
            return Ok(new { success = true, data = result, message = (string?)null });
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateCouponRequest request, CancellationToken cancellationToken)
        {
            var result = await _service.CreateAsync(request, cancellationToken);
            return StatusCode(201, new { success = true, data = result, message = (string?)null });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, UpdateCouponRequest request, CancellationToken cancellationToken)
        {
            var result = await _service.UpdateAsync(id, request, cancellationToken);
            return Ok(new { success = true, data = result, message = (string?)null });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            await _service.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
    }

}
