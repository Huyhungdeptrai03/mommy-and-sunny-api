using MommyAndSunny.Domain.Common;

namespace MommyAndSunny.Domain.Entities;

public class OrderItem : BaseEntity
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    // Vẫn giữ FK để tham chiếu, nhưng KHÔNG dùng để hiển thị (dùng bản snapshot bên dưới)
    public Guid ProductVariantId { get; set; }
    public ProductVariant ProductVariant { get; set; } = null!;

    // Snapshot tại thời điểm đặt hàng — nếu sau này admin đổi tên/giá sản phẩm,
    // đơn hàng cũ vẫn hiển thị đúng thông tin lúc khách mua
    public string ProductNameSnapshot { get; set; } = string.Empty;
    public string SkuSnapshot { get; set; } = string.Empty;
    public string? VariantDescriptionSnapshot { get; set; } // VD: "Size M, Xanh dương"

    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal Subtotal { get; set; }

    // Snapshot đợt hàng tại thời điểm đặt — NULL nghĩa là hàng có sẵn (mua ngay),
    // có giá trị nghĩa là hàng đặt trước thuộc 1 đợt xách tay cụ thể. Đây là cơ sở để
    // xác định 1 ĐƠN HÀNG có cần đi qua luồng trạng thái mở rộng (Purchasing -> ...) hay không
    public Guid? PurchaseBatchId { get; set; }
    public string? PurchaseBatchNameSnapshot { get; set; }
}