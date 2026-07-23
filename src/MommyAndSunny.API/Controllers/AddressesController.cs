using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MommyAndSunny.Application.Account.DTOs;
using MommyAndSunny.Application.Account.Services;

namespace MommyAndSunny.API.Controllers
{
    [ApiController]
    [Route("api/account/addresses")]
    [Authorize]
    public class AddressesController : ControllerBase
    {
        private readonly IAddressService _addressService;

        public AddressesController(IAddressService addressService)
        {
            _addressService = addressService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyAddresses(CancellationToken cancellationToken)
        {
            var result = await _addressService.GetMyAddressesAsync(cancellationToken);
            return Ok(new { success = true, data = result, message = (string?)null });
        }

        [HttpPost]
        public async Task<IActionResult> CreateAddress(CreateAddressRequest request, CancellationToken cancellationToken)
        {
            var result = await _addressService.CreateAddressAsync(request, cancellationToken);
            return StatusCode(201, new { success = true, data = result, message = (string?)null });
        }
    }
}
