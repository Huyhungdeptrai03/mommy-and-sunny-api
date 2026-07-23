namespace MommyAndSunny.Domain.Enums;

// Trạng thái CỦA MỘT GIAO DỊCH cụ thể qua cổng thanh toán (Momo/VNPay).
// Khác với OrderPaymentStatus (trạng thái thanh toán tổng của cả đơn hàng).
public enum PaymentTransactionStatus
{
    Pending,
    Success,
    Failed
}
