namespace MommyAndSunny.Domain.Enums;

// Trạng thái thanh toán CỦA ĐƠN HÀNG — độc lập với OrderStatus.
// VD: đơn có thể đang "Shipping" nhưng "Unpaid" nếu thanh toán COD.
public enum OrderPaymentStatus
{
    Unpaid,
    Paid,
    Failed,
    Refunded
}
