using MommyAndSunny.Domain.Common;
namespace MommyAndSunny.Domain.Entities;

// No provider tokens or plaintext browser/handoff secrets are persisted.
public class ExternalHandoff : BaseEntity
{
    public string CodeHash { get; set; } = "";
    public string BrowserHash { get; set; } = "";
    public string Provider { get; set; } = "";
    public string ProviderSubject { get; set; } = "";
    public string? ProviderEmail { get; set; }
    public string FullName { get; set; } = "";
    public string? PendingEmail { get; set; }
    public string? EmailTokenHash { get; set; }
    public bool EmailVerified { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConsumedAt { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();
}
