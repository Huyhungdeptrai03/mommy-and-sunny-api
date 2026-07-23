using MommyAndSunny.Domain.Common;
using MommyAndSunny.Domain.Enums;

namespace MommyAndSunny.Domain.Entities;

public class Product : BaseEntity
{
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Brand { get; set; }

    // Đặc thù ngành mẹ & bé — dùng để lọc sản phẩm theo tháng tuổi bé
    public int AgeRangeMinMonths { get; set; }
    public int AgeRangeMaxMonths { get; set; }

    // Draft -> Published -> Discontinued (không xóa cứng để giữ lịch sử đơn hàng)
    public ProductStatus Status { get; set; } = ProductStatus.Draft;

    // Navigation properties
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
}
