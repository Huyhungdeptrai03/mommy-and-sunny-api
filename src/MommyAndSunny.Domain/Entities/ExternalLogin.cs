using MommyAndSunny.Domain.Common;
namespace MommyAndSunny.Domain.Entities;

public class ExternalLogin : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string Provider { get; set; } = "";
    public string ProviderSubject { get; set; } = "";
    public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;
}
