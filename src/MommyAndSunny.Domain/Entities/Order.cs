using MommyAndSunny.Domain.Common;
using MommyAndSunny.Domain.Enums;

namespace MommyAndSunny.Domain.Entities;

public class Order : BaseEntity
{
    // Mã hiển thị cho khách, VD: MS20260719001 — khác với Id (Guid) nội bộ
    public string OrderCode { get; set; } = string.Empty;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    // Snapshot thông tin giao hàng tại thời điểm đặt — CỐ Ý không chỉ lưu FK đến Address,
    // vì khách có thể sửa/xóa địa chỉ sau này nhưng đơn hàng cũ phải giữ nguyên thông tin đã giao
    public string RecipientName { get; set; } = string.Empty;
    public string RecipientPhone { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;

    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public PaymentMethod PaymentMethod { get; set; }
    public OrderPaymentStatus PaymentStatus { get; set; } = OrderPaymentStatus.Unpaid;

    public decimal SubTotal { get; set; }
    public decimal ShippingFee { get; set; } = 0;
    public decimal DiscountAmount { get; set; } = 0;
    public decimal TotalAmount { get; set; }

    public string? CustomerNote { get; set; }

    // Navigation properties
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<OrderStatusHistory> StatusHistory { get; set; } = new List<OrderStatusHistory>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
