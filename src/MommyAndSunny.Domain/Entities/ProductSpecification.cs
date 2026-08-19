using MommyAndSunny.Domain.Common;

namespace MommyAndSunny.Domain.Entities;

public class ProductSpecification : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int DisplayOrder { get; set; } = 0;
}
