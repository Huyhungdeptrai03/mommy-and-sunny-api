using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MommyAndSunny.Application.Common.Interfaces;
namespace MommyAndSunny.API.Services;

public sealed class SessionValidationService(IApplicationDbContext db)
{
    public async Task<bool> IsValidAsync(ClaimsPrincipal? principal, CancellationToken ct = default)
    {
        if (!Guid.TryParse(principal?.FindFirstValue("sid"), out var sid) ||
            !Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ||
            !Guid.TryParse(principal.FindFirstValue("auth_ver"), out var version)) return false;
        var session = await db.AuthSessions.AsNoTracking().Where(s => s.Id == sid && s.UserId == uid && s.RevokedAt == null
            && s.ExpiresAt > DateTime.UtcNow && s.SecurityVersion == version && s.User.SecurityVersion == version)
            .Select(s => new { s.User.Role }).SingleOrDefaultAsync(ct);
        return session != null && session.Role.ToString() == principal.FindFirstValue(ClaimTypes.Role);
    }
}
