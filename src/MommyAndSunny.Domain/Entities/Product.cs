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

    // Tùy chọn — chỉ có ý nghĩa với sản phẩm mẹ & bé, để trống với các ngành hàng khác
    // (đồ gia dụng, mỹ phẩm, thời trang...) khi website mở rộng đa ngành hàng
    public int? AgeRangeMinMonths { get; set; }
    public int? AgeRangeMaxMonths { get; set; }

    public ProductStatus Status { get; set; } = ProductStatus.Draft;

    // Tùy chọn — có giá trị nghĩa là sản phẩm thuộc 1 đợt hàng xách tay cụ thể
    // (đặt trước, chưa có hàng thật), NULL nghĩa là sản phẩm bán ngay như bình thường
    public Guid? PurchaseBatchId { get; set; }
    public PurchaseBatch? PurchaseBatch { get; set; }

    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public ICollection<ProductSpecification> Specifications { get; set; } = new List<ProductSpecification>();
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
}
