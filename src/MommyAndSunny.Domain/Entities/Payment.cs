using MommyAndSunny.Domain.Common;
using MommyAndSunny.Domain.Enums;

namespace MommyAndSunny.Domain.Entities;

public class Payment : BaseEntity
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public PaymentMethod Provider { get; set; }
    public string? TransactionId { get; set; } // Mã giao dịch từ cổng thanh toán
    public decimal Amount { get; set; }
    public PaymentTransactionStatus Status { get; set; } = PaymentTransactionStatus.Pending;

    // Lưu response gốc từ cổng thanh toán (map sang kiểu jsonb ở Infrastructure) — phục vụ debug
    public string? RawCallbackData { get; set; }
    public DateTime? PaidAt { get; set; }
}
