using MommyAndSunny.Domain.Common;
using MommyAndSunny.Domain.Enums;

namespace MommyAndSunny.Domain.Entities;

public class OrderStatusHistory : BaseEntity
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    // Null nếu đây là bản ghi khởi tạo (đơn hàng vừa được tạo)
    public OrderStatus? FromStatus { get; set; }
    public OrderStatus ToStatus { get; set; }

    // Null nếu do hệ thống tự động đổi (không phải admin thao tác)
    public Guid? ChangedByUserId { get; set; }
    public User? ChangedByUser { get; set; }

    public string? Note { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
