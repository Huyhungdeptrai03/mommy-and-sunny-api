using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MommyAndSunny.Application.Common.Interfaces;
using MommyAndSunny.Application.Common.Payments.DTOs;
using MommyAndSunny.Domain.Enums;

namespace MommyAndSunny.API.Controllers
{
    [ApiController]
    [Route("api/payments/momo")]
    public class PaymentsController : ControllerBase
    {
        private readonly IApplicationDbContext _context;
        private readonly IMomoPaymentService _momoPaymentService;

        public PaymentsController(IApplicationDbContext context, IMomoPaymentService momoPaymentService)
        {
            _context = context;
            _momoPaymentService = momoPaymentService;
        }

        // Endpoint này do MOMO gọi (server-to-server), không phải người dùng —
        // KHÔNG có [Authorize], nhưng bảo mật bằng cách xác thực chữ ký MoMo gửi kèm
        [HttpPost("ipn")]
        public async Task<IActionResult> HandleIpn([FromBody] MomoIpnRequest request, CancellationToken cancellationToken)
        {
            if (!_momoPaymentService.VerifyIpnSignature(request))
            {
                return BadRequest(new { message = "Chữ ký không hợp lệ" });
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderCode == request.OrderId, cancellationToken);

            if (order is null)
            {
                return NotFound(new { message = "Không tìm thấy đơn hàng" });
            }

            // resultCode = 0 nghĩa là giao dịch thành công (quy ước của MoMo)
            if (request.ResultCode == 0)
            {
                order.PaymentStatus = OrderPaymentStatus.Paid;
                await _context.SaveChangesAsync(cancellationToken);
            }

            // MoMo yêu cầu server PHẢI trả về 204/200 nhanh chóng, nếu không họ sẽ coi là
            // thất bại và gọi lại IPN nhiều lần — luôn trả 200 dù đơn hàng không tìm thấy
            // ở các nhánh phía trên cũng nên cân nhắc, nhưng ở đây giữ rõ ràng cho dễ debug
            return Ok(new { message = "Đã nhận IPN" });
        }
    }
}
