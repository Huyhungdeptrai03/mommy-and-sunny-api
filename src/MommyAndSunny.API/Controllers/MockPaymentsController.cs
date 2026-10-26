using Microsoft.AspNetCore.Mvc;
using MommyAndSunny.Infrastructure.ExternalServices;

namespace MommyAndSunny.API.Controllers;

[ApiController, Route("api/payments/mock-qr")]
public sealed class MockPaymentsController(IHostEnvironment environment, IConfiguration configuration) : ControllerBase
{
    [HttpGet]
    public IActionResult Image([FromQuery] string orderCode, [FromQuery] decimal amount)
    {
        if (!environment.IsDevelopment() || !configuration.GetValue<bool>("SePay:UseMock")) return NotFound();
        if (string.IsNullOrWhiteSpace(orderCode) || orderCode.Length > 128 || amount <= 0 || amount != decimal.Truncate(amount))
            return BadRequest();
        Response.Headers.CacheControl = "no-store";
        Response.Headers["Content-Security-Policy"] = "default-src 'none'; sandbox";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return Content(MockSePayPaymentGateway.RenderPlaceholder(orderCode, amount), "image/svg+xml", System.Text.Encoding.UTF8);
    }
}
