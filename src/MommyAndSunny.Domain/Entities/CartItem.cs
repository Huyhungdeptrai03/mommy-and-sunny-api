using MommyAndSunny.Domain.Common;

namespace MommyAndSunny.Domain.Entities;

public class CartItem : BaseEntity
{
    public Guid CartId { get; set; }
    public Cart Cart { get; set; } = null!;

    // Mỗi dòng là 1 BIẾN THỂ cụ thể, không phải sản phẩm chung chung
    public Guid ProductVariantId { get; set; }
    public ProductVariant ProductVariant { get; set; } = null!;

    public int Quantity { get; set; }
}
