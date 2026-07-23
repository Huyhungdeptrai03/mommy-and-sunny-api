using MommyAndSunny.Domain.Common;

namespace MommyAndSunny.Domain.Entities;

public class ProductVariant : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public string Sku { get; set; } = string.Empty;
    public string? Size { get; set; }
    public string? Color { get; set; }
    public decimal Price { get; set; }

    // Tồn kho thực tế trong kho — chỉ trừ khi Order chuyển sang trạng thái Confirmed
    public int StockQuantity { get; set; } = 0;

    // Số lượng đang bị giữ chỗ bởi các đơn hàng ở trạng thái Pending (chưa xác nhận)
    public int ReservedQuantity { get; set; } = 0;

    // Tồn kho khả dụng hiển thị cho khách hàng — property tính toán, KHÔNG map vào cột database
    // (EF Core Configuration sẽ đánh dấu Ignore() cho property này)
    public int AvailableQuantity => StockQuantity - ReservedQuantity;

    // Navigation properties
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
