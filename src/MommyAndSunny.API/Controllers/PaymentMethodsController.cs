using Microsoft.AspNetCore.Mvc;
using MommyAndSunny.Application.Common.Interfaces;

namespace MommyAndSunny.API.Controllers;

[ApiController, Route("api/payments/methods")]
public sealed class PaymentMethodsController(ISePayPaymentService sePay) : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        Response.Headers.CacheControl = "no-store";
        return Ok(new { success = true, data = new { paymentMethods = sePay.IsEnabled
            ? new[] { "COD", "SePay" } : new[] { "COD" } }, message = (string?)null });
    }
}
