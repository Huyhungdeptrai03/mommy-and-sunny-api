using Microsoft.AspNetCore.Mvc;
using MommyAndSunny.Application.PurchaseBatches.Services;

namespace MommyAndSunny.API.Controllers
{
    [ApiController]
    [Route("api/purchase-batches")]
    public class PurchaseBatchesController : ControllerBase
    {
        private readonly IPublicPurchaseBatchService _service;

        public PurchaseBatchesController(IPublicPurchaseBatchService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetOpenBatches(CancellationToken cancellationToken)
        {
            var result = await _service.GetOpenBatchesAsync(cancellationToken);
            return Ok(new { success = true, data = result, message = (string?)null });
        }
    }
}
