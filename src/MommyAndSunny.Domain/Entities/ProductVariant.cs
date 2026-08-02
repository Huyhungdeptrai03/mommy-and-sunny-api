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

    // Flash Sale — CompareAtPrice là "giá gốc" hiển thị gạch ngang, chỉ có ý nghĩa
    // khi khác null VÀ lớn hơn Price. SaleEndsAt xác định thời điểm sale kết thúc —
    // sản phẩm chỉ được coi là "đang Flash Sale" khi CẢ 2 điều kiện đều thỏa mãn
    // VÀ SaleEndsAt còn ở tương lai (xem logic ở ProductService.GetFlashSaleAsync)
    public decimal? CompareAtPrice { get; set; }
    public DateTime? SaleEndsAt { get; set; }

    // Tồn kho thực tế trong kho — chỉ trừ khi Order chuyển sang trạng thái Confirmed
    public int StockQuantity { get; set; } = 0;

    // Số lượng đang bị giữ chỗ bởi các đơn hàng ở trạng thái Pending (chưa xác nhận)
    public int ReservedQuantity { get; set; } = 0;

    // Tồn kho khả dụng hiển thị cho khách hàng — property tính toán, KHÔNG map vào cột database
    public int AvailableQuantity => StockQuantity - ReservedQuantity;

    // Navigation properties
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
