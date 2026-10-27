using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using MommyAndSunny.Application.Common.Interfaces;
using MommyAndSunny.Application.Common.Payments.DTOs;
using MommyAndSunny.Domain.Enums;
using MommyAndSunny.Domain.Entities;
using System.Text.Json;
using Npgsql;

namespace MommyAndSunny.API.Controllers
{
    [ApiController]
    [Route("api/payments/sepay")]
    public class PaymentsController : ControllerBase
    {
        private readonly IApplicationDbContext _context;
        private readonly ISePayPaymentService _sePayPaymentService;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(
            IApplicationDbContext context,
            ISePayPaymentService sePayPaymentService,
            ILogger<PaymentsController> logger)
        {
            _context = context;
            _sePayPaymentService = sePayPaymentService;
            _logger = logger;
        }

        [HttpPost("webhook")]
        [EnableRateLimiting("payment-callbacks")]
        public async Task<IActionResult> HandleWebhook([FromBody] SePayWebhookRequest request, CancellationToken cancellationToken)
        {
            if (!_sePayPaymentService.IsEnabled)
                return NotFound();

            if (!_sePayPaymentService.VerifyWebhookAuthorization(HttpContext.Request.Headers.Authorization.ToString()))
            {
                _logger.LogWarning("Từ chối SePay IPN có chữ ký không hợp lệ cho đơn {OrderId}", request.Code);
                return BadRequest(new { success = false, message = "Chữ ký không hợp lệ" });
            }

            if (!_sePayPaymentService.IsReceivingAccount(request.AccountNumber))
                return BadRequest(new { success = false, message = "Sai t?i kho?n nh?n" });
            if (request.TransferType != "in")
                return Ok(new { success = true });
            if (request.Id <= 0)
                return BadRequest(new { success = false });

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderCode == request.Code, cancellationToken);

            if (order is null)
            {
                _logger.LogWarning("Nhận SePay IPN cho đơn không tồn tại {OrderId}", request.Code);
                return Ok(new { success = true, message = "Đã nhận IPN" });
            }

            if (request.TransferType == "in")
            {
                if (order.PaymentMethod != PaymentMethod.SePay ||
                    order.Status == OrderStatus.Cancelled ||
                    request.TransferAmount <= 0 ||
                    request.TransferAmount != order.TotalAmount)
                {
                    _logger.LogWarning(
                        "Từ chối SePay IPN không khớp đơn {OrderCode}: amount={Amount}, method={Method}, status={Status}",
                        order.OrderCode, request.TransferAmount, order.PaymentMethod, order.Status);
                    return BadRequest(new { success = false, message = "Thông tin giao dịch không khớp với đơn hàng" });
                }

                var transactionId = request.Id.ToString();
                var transaction = await _context.Payments.FirstOrDefaultAsync(
                    p => p.Provider == PaymentMethod.SePay && p.TransactionId == transactionId,
                    cancellationToken);

                if (transaction is not null)
                {
                    if (transaction.OrderId == order.Id)
                    {
                        return Ok(new { success = true, message = "Giao dịch đã được xử lý" });
                    }

                    _logger.LogWarning("SePay transaction {TransactionId} đã thuộc về một đơn khác", transactionId);
                    return BadRequest(new { success = false, message = "Mã giao dịch đã được sử dụng" });
                }

                if (order.PaymentStatus == OrderPaymentStatus.Paid)
                {
                    _logger.LogWarning("Đơn {OrderCode} đã thanh toán nhưng nhận transaction mới {TransactionId}",
                        order.OrderCode, transactionId);
                    return BadRequest(new { success = false, message = "Đơn hàng đã được thanh toán bởi giao dịch khác" });
                }

                _context.Payments.Add(new Payment
                {
                    OrderId = order.Id,
                    Provider = PaymentMethod.SePay,
                    TransactionId = transactionId,
                    Amount = request.TransferAmount,
                    Status = PaymentTransactionStatus.Success,
                    RawCallbackData = JsonSerializer.Serialize(request),
                    PaidAt = DateTime.UtcNow
                });
                order.PaymentStatus = OrderPaymentStatus.Paid;
                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException exception) when (
                    exception.InnerException is PostgresException
                    {
                        SqlState: PostgresErrorCodes.UniqueViolation,
                        ConstraintName: "IX_Payments_Provider_TransactionId"
                    })
                {
                    _logger.LogInformation("SePay transaction {TransactionId} đã được callback khác xử lý đồng thời",
                        transactionId);
                    return Ok(new { success = true, message = "Giao dịch đã được xử lý" });
                }
            }
            return Ok(new { success = true, message = "Đã nhận IPN" });
        }
    }
}
