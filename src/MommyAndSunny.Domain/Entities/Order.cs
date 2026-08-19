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

    // Snapshot mã giảm giá đã dùng — giữ FK để kiểm tra "khách này đã dùng mã X chưa"
    // (query theo CouponId), nhưng CŨNG lưu snapshot CouponCode dạng chuỗi để hiển thị
    // đúng lịch sử đơn hàng ngay cả khi Admin xóa hẳn Coupon gốc sau này
    public Guid? CouponId { get; set; }
    public Coupon? Coupon { get; set; }
    public string? CouponCode { get; set; }

    // ===== Cọc — chỉ có ý nghĩa với đơn có sản phẩm thuộc đợt hàng xách tay =====
    // Admin tự nhập số tiền cọc và tự đánh dấu đã thu (thu ngoài hệ thống, không qua
    // cổng thanh toán) — theo đúng quyết định nghiệp vụ đã thống nhất
    public decimal? DepositAmount { get; set; }
    public OrderPaymentStatus DepositPaymentStatus { get; set; } = OrderPaymentStatus.Unpaid;

    public string? CustomerNote { get; set; }

    // Navigation properties
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<OrderStatusHistory> StatusHistory { get; set; } = new List<OrderStatusHistory>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}