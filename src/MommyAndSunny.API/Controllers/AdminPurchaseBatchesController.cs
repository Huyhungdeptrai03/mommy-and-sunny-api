using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MommyAndSunny.Application.Admin.PurchaseBatches.DTOs;
using MommyAndSunny.Application.Admin.PurchaseBatches.Services;

namespace MommyAndSunny.API.Controllers
{
    [ApiController]
    [Route("api/admin/purchase-batches")]
    [Authorize(Roles = "Admin")]
    public class AdminPurchaseBatchesController : ControllerBase
    {
        private readonly IAdminPurchaseBatchService _service;

        public AdminPurchaseBatchesController(IAdminPurchaseBatchService service)
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
        public async Task<IActionResult> Create(CreatePurchaseBatchRequest request, CancellationToken cancellationToken)
        {
            var result = await _service.CreateAsync(request, cancellationToken);
            return StatusCode(201, new { success = true, data = result, message = (string?)null });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            Guid id, UpdatePurchaseBatchRequest request, CancellationToken cancellationToken)
        {
            var result = await _service.UpdateAsync(id, request, cancellationToken);
            return Ok(new { success = true, data = result, message = (string?)null });
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(
            Guid id, UpdatePurchaseBatchStatusRequest request, CancellationToken cancellationToken)
        {
            var result = await _service.UpdateStatusAsync(id, request, cancellationToken);
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
