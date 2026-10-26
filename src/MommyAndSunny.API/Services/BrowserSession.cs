using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MommyAndSunny.Application.Auth.DTOs;
namespace MommyAndSunny.API.Services;

public sealed class BrowserSession(IConfiguration config, IAntiforgery antiforgery)
{
    public const string RefreshCookie = "__Host-ms-refresh";
    public static CookieOptions Cookie(DateTimeOffset? expires = null) => new() {
        Secure = true, HttpOnly = true, SameSite = SameSiteMode.Lax, Path = "/", Expires = expires, IsEssential = true
    };
    public bool ValidOrigin(HttpRequest request) => Uri.TryCreate(config["Frontend:BaseUrl"], UriKind.Absolute, out var expected)
        && request.Headers.Origin.Count == 1
        && string.Equals(request.Headers.Origin.ToString(), expected.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase);
    public async Task<bool> ValidateAsync(HttpContext context)
    {
        // Fail closed for absent/null Origin. Non-browser clients must explicitly implement this protocol too.
        if (!context.Request.IsHttps || !ValidOrigin(context.Request)) return false;
        try { await antiforgery.ValidateRequestAsync(context); return true; }
        catch (AntiforgeryValidationException) { return false; }
    }
    public void Write(HttpResponse response, AuthResponse result)
    {
        response.Headers.CacheControl = "no-store";
        response.Cookies.Append(RefreshCookie, result.RefreshToken, Cookie(new DateTimeOffset(result.RefreshTokenExpiresAt)));
    }
    public void Clear(HttpResponse response)
    {
        response.Headers.CacheControl = "no-store";
        response.Cookies.Delete(RefreshCookie, Cookie());
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class BrowserCsrfAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (!await context.HttpContext.RequestServices.GetRequiredService<BrowserSession>().ValidateAsync(context.HttpContext))
            context.Result = new ObjectResult(new { success = false, data = (object?)null,
                code = "FORBIDDEN", message = "Origin hoặc CSRF token không hợp lệ." }) { StatusCode = 403 };
    }
}
