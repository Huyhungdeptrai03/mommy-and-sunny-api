using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MommyAndSunny.Application.Orders.DTOs;
using MommyAndSunny.Application.Orders.Services;

namespace MommyAndSunny.API.Controllers
{
    [ApiController]
    [Route("api/orders")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder(CreateOrderRequest request, CancellationToken cancellationToken)
        {
            var result = await _orderService.CreateOrderAsync(request, cancellationToken);
            return StatusCode(201, new { success = true, data = result, message = (string?)null });
        }

        [HttpGet]
        public async Task<IActionResult> GetMyOrders([FromQuery] OrderFilterRequest filter, CancellationToken cancellationToken)
        {
            var result = await _orderService.GetMyOrdersAsync(filter, cancellationToken);
            return Ok(new { success = true, data = result, message = (string?)null });
        }

        [HttpGet("{orderCode}")]
        public async Task<IActionResult> GetOrderByCode(string orderCode, CancellationToken cancellationToken)
        {
            var result = await _orderService.GetOrderByCodeAsync(orderCode, cancellationToken);
            return Ok(new { success = true, data = result, message = (string?)null });
        }

        [HttpPost("{orderCode}/cancel")]
        public async Task<IActionResult> CancelOrder(string orderCode, CancellationToken cancellationToken)
        {
            await _orderService.CancelOrderAsync(orderCode, cancellationToken);
            return Ok(new { success = true, data = (object?)null, message = "Đã hủy đơn hàng" });
        }
    }
}
