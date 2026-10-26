using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace MommyAndSunny.API.Services;

// Separate credential abuse from normal cookie recovery / authenticated reads.
public static class MemberAuthRateLimits
{
    public const string Credentials = "authentication";
    public const string Sensitive = "sensitive-authentication";
    public const string Csrf = "session-csrf";
    public const string Refresh = "session-refresh";
    public const string Read = "session-read";
    public const string Logout = "session-logout";
    public const string OAuth = "oauth-exchange";
    public const string OAuthStart = "oauth-start";
    public const string OAuthEmail = "oauth-email";

    public static void AddPolicies(RateLimiterOptions options)
    {
        // Preserve existing login and recovery/mail brute-force budgets.
        Add(options, Credentials, 20, TimeSpan.FromMinutes(5));
        Add(options, Sensitive, 5, TimeSpan.FromMinutes(15));
        Add(options, Csrf, 20, TimeSpan.FromMinutes(5));
        // Minimum JWT TTL is 60s; frontend renews 30s early. Three active tabs
        // need <= 3 bootstraps + 3 * 10 rotations / 5m. 40 covers that envelope,
        // remains IP-bounded, and cannot be bypassed by rotating cookie values.
        Add(options, Refresh, 40, TimeSpan.FromMinutes(5));
        // One check/minute/tab: 3 tabs use 15/5m; partition validated members
        // independently so unrelated users behind NAT do not share read quota.
        Add(options, Read, 20, TimeSpan.FromMinutes(5), authenticated: true);
        Add(options, Logout, 20, TimeSpan.FromMinutes(5));
        Add(options, OAuthStart, 5, TimeSpan.FromMinutes(15));
        Add(options, OAuth, 20, TimeSpan.FromMinutes(5));
        Add(options, OAuthEmail, 5, TimeSpan.FromMinutes(15));
    }

    private static void Add(RateLimiterOptions options, string name, int permits, TimeSpan window, bool authenticated = false)
        => options.AddPolicy(name, context => RateLimitPartition.GetFixedWindowLimiter(
            name + ":" + (authenticated && context.User.Identity?.IsAuthenticated == true
                ? "member:" + context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                : "ip:" + (context.Connection.RemoteIpAddress?.ToString() ?? "unknown")),
            _ => new FixedWindowRateLimiterOptions { PermitLimit = permits, Window = window, QueueLimit = 0 }));

    public static async ValueTask RejectAsync(OnRejectedContext context, CancellationToken ct)
    {
        var delay = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var remaining)
            ? Math.Max(1, (int)Math.Ceiling(remaining.TotalSeconds)) : 60;
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.Headers.RetryAfter = delay.ToString(CultureInfo.InvariantCulture);
        context.HttpContext.Response.Headers.CacheControl = "no-store";
        await context.HttpContext.Response.WriteAsJsonAsync(new { success = false, data = (object?)null,
            code = "RATE_LIMITED", message = "Vui lòng thử lại sau." }, ct);
    }
}
