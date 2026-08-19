using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MommyAndSunny.Application.Coupons.DTOs;
using MommyAndSunny.Application.Coupons.Services;

namespace MommyAndSunny.API.Controllers
{
    [ApiController]
    [Route("api/coupons")]
    [Authorize]
    public class CouponsController : ControllerBase
    {
        private readonly ICouponService _couponService;

        public CouponsController(ICouponService couponService)
        {
            _couponService = couponService;
        }

        [HttpPost("apply")]
        public async Task<IActionResult> Apply(ApplyCouponRequest request, CancellationToken cancellationToken)
        {
            var result = await _couponService.PreviewApplyAsync(request.Code, cancellationToken);
            return Ok(new { success = true, data = result, message = (string?)null });
        }
    }
}
