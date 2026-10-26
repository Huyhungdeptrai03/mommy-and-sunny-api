using MommyAndSunny.Domain.Common;
namespace MommyAndSunny.Domain.Entities;

public class AuthSession : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid SecurityVersion { get; set; }
    public User User { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();
}
