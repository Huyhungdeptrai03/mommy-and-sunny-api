using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MommyAndSunny.Application.Admin.Orders.DTOs;
using MommyAndSunny.Application.Admin.Orders.Service;

namespace MommyAndSunny.API.Controllers
{
    [ApiController]
    [Route("api/admin/orders")]
    [Authorize(Roles = "Admin")]
    public class AdminOrdersController : ControllerBase
    {
        private readonly IAdminOrderService _adminOrderService;

        public AdminOrdersController(IAdminOrderService adminOrderService)
        {
            _adminOrderService = adminOrderService;
        }

        [HttpGet]
        public async Task<IActionResult> GetOrders([FromQuery] AdminOrderFilterRequest filter, CancellationToken cancellationToken)
        {
            var result = await _adminOrderService.GetOrdersAsync(filter, cancellationToken);
            return Ok(new { success = true, data = result, message = (string?)null });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _adminOrderService.GetOrderByIdAsync(id, cancellationToken);
            return Ok(new { success = true, data = result, message = (string?)null });
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(Guid id, UpdateOrderStatusRequest request, CancellationToken cancellationToken)
        {
            await _adminOrderService.UpdateOrderStatusAsync(id, request, cancellationToken);
            return Ok(new { success = true, data = (object?)null, message = "Cập nhật trạng thái đơn hàng thành công" });
        }

        [HttpPut("{id}/deposit")]
        public async Task<IActionResult> SetDeposit(Guid id, SetOrderDepositRequest request, CancellationToken cancellationToken)
        {
            var result = await _adminOrderService.SetDepositAsync(id, request.DepositAmount, cancellationToken);
            return Ok(new { success = true, data = result, message = "Đã đặt số tiền cọc" });
        }

        [HttpPost("{id}/deposit/mark-paid")]
        public async Task<IActionResult> MarkDepositPaid(Guid id, CancellationToken cancellationToken)
        {
            var result = await _adminOrderService.MarkDepositPaidAsync(id, cancellationToken);
            return Ok(new { success = true, data = result, message = "Đã đánh dấu đã thu cọc" });
        }

        [HttpPost("{id}/mark-paid")]
        public async Task<IActionResult> MarkFinalPaymentPaid(Guid id, CancellationToken cancellationToken)
        {
            var result = await _adminOrderService.MarkFinalPaymentPaidAsync(id, cancellationToken);
            return Ok(new { success = true, data = result, message = "Đã đánh dấu đã thanh toán đủ" });
        }
    }
}
