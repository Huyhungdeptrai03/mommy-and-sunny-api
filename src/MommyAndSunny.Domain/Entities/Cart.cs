using MommyAndSunny.Domain.Common;

namespace MommyAndSunny.Domain.Entities;

public class Cart : BaseEntity
{
    // 1 user chỉ có 1 giỏ hàng active (UNIQUE constraint ở tầng database)
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}
