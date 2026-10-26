using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using MommyAndSunny.API.Services;
using MommyAndSunny.Application.Auth.Services;
using MommyAndSunny.Application.Common.Interfaces;
using MommyAndSunny.Domain.Entities;
using MommyAndSunny.Domain.Enums;
using MommyAndSunny.Domain.Exceptions;
using MommyAndSunny.Infrastructure.Persistence;
namespace MommyAndSunny.API.Controllers;

[ApiController, Route("api/auth/external"), EnableRateLimiting(MemberAuthRateLimits.OAuth)]
public sealed class ExternalAuthController(ApplicationDbContext db, IAuthService auth, BrowserSession browser,
    IEmailService email, IConfiguration config, IAuthenticationSchemeProvider schemes) : ControllerBase
{
    public const string ExternalScheme = "ExternalTemporary";
    public const string TransactionCookie = "__Host-ms-oauth";

    [HttpGet("{provider}/start"), EnableRateLimiting(MemberAuthRateLimits.OAuthStart)]
    public async Task<IActionResult> Start(string provider)
    {
        if (provider is not ("google" or "facebook")) return NotFound();
        if (!Request.IsHttps || Request.Headers["Sec-Fetch-Site"] != "same-origin")
            throw new AuthException("FORBIDDEN", 403, "Bắt đầu đăng nhập từ website chính thức.");
        var scheme = provider == "google" ? "Google" : "Facebook";
        if (await schemes.GetSchemeAsync(scheme) is null)
            throw new AuthException("PROVIDER_NOT_CONFIGURED", 503, "Nhà cung cấp chưa được cấu hình.");
        var binding = AuthSecurity.RandomToken();
        Response.Cookies.Append(TransactionCookie, binding, BrowserSession.Cookie(DateTimeOffset.UtcNow.AddMinutes(10)));
        var properties = new AuthenticationProperties { RedirectUri = "/api/auth/external/complete", IsPersistent = false };
        properties.Items["browser_hash"] = AuthSecurity.Hash(binding);
        properties.Items["provider"] = provider;
        return Challenge(properties, scheme);
    }

    [HttpGet("complete")]
    public async Task<IActionResult> Complete(CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        var external = await HttpContext.AuthenticateAsync(ExternalScheme);
        await HttpContext.SignOutAsync(ExternalScheme);
        var binding = Request.Cookies[TransactionCookie];
        var provider = external.Properties?.Items.TryGetValue("provider", out var providerValue) == true ? providerValue : null;
        var subject = external.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!external.Succeeded || string.IsNullOrEmpty(binding) || provider is not ("google" or "facebook") ||
            external.Properties?.Items.TryGetValue("browser_hash", out var browserHash) != true || browserHash != AuthSecurity.Hash(binding) ||
            string.IsNullOrEmpty(subject) || subject.Length > 255)
            throw new AuthException("INVALID_OAUTH_TRANSACTION", 400, "Giao dịch đăng nhập không hợp lệ.");
        var code = AuthSecurity.RandomToken();
        var providerEmail = external.Principal?.FindFirstValue(ClaimTypes.Email);
        db.ExternalHandoffs.Add(new ExternalHandoff {
            CodeHash = AuthSecurity.Hash(code), BrowserHash = AuthSecurity.Hash(binding), Provider = provider,
            ProviderSubject = subject, ProviderEmail = providerEmail?.Length <= 255 ? providerEmail : null,
            FullName = (external.Principal?.FindFirstValue(ClaimTypes.Name) ?? "")[..Math.Min(150, (external.Principal?.FindFirstValue(ClaimTypes.Name) ?? "").Length)],
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        });
        await db.SaveChangesAsync(ct);
        // Fixed same-origin frontend path. No caller-controlled return URL or token in URL.
        return Redirect("/auth/callback?code=" + Uri.EscapeDataString(code));
    }

    private async Task<ExternalHandoff> Pending(string code, CancellationToken ct)
    {
        var binding = Request.Cookies[TransactionCookie];
        if (string.IsNullOrEmpty(binding)) throw new AuthException("INVALID_HANDOFF", 400, "Thiếu giao dịch trình duyệt.");
        var hash = AuthSecurity.Hash(code);
        var h = await db.ExternalHandoffs.SingleOrDefaultAsync(x => x.CodeHash == hash, ct);
        if (h is null || h.ConsumedAt != null || h.ExpiresAt <= DateTime.UtcNow || h.BrowserHash != AuthSecurity.Hash(binding))
            throw new AuthException("INVALID_HANDOFF", 400, "Mã đã dùng hoặc hết hạn.");
        return h;
    }

    [HttpPost("email"), BrowserCsrf, EnableRateLimiting(MemberAuthRateLimits.OAuthEmail)]
    public async Task<IActionResult> SendOnboardingEmail(OnboardingEmail request, CancellationToken ct)
    {
        var h = await Pending(request.Code, ct);
        if (string.IsNullOrWhiteSpace(request.FullName)) throw new DomainException("Họ tên không được để trống.");
        var raw = AuthSecurity.RandomToken();
        h.PendingEmail = AuthSecurity.NormalizeEmail(request.Email);
        h.FullName = request.FullName.Trim();
        h.EmailTokenHash = AuthSecurity.Hash(raw);
        h.EmailVerified = false;
        h.Version = Guid.NewGuid();
        // Remains capped by the initiating browser cookie's 10-minute lifetime.
        h.ExpiresAt = DateTime.UtcNow.AddMinutes(5);
        await db.SaveChangesAsync(ct);
        var link = $"{config["Frontend:BaseUrl"]!.TrimEnd('/')}/auth/callback?code={Uri.EscapeDataString(request.Code)}&emailToken={Uri.EscapeDataString(raw)}";
        await email.SendVerificationEmailAsync(h.PendingEmail, h.FullName, link, ct);
        return Accepted(new { success = true, data = (object?)null, message = "Vui lòng xác minh email trên cùng trình duyệt." });
    }

    [HttpPost("exchange"), BrowserCsrf]
    public async Task<IActionResult> Exchange(ExternalExchange request, CancellationToken ct)
    {
        var h = await Pending(request.Code, ct);
        var existing = await db.ExternalLogins.Include(x => x.User)
            .SingleOrDefaultAsync(x => x.Provider == h.Provider && x.ProviderSubject == h.ProviderSubject, ct);
        User user;
        if (existing != null)
        {
            if (request.LinkExistingAccount && existing.UserId != CurrentId())
                throw new AuthException("IDENTITY_CONFLICT", 409, "Danh tính đã liên kết tài khoản khác.");
            user = existing.User;
            existing.LastUsedAt = DateTime.UtcNow;
        }
        else if (request.LinkExistingAccount)
        {
            var uid = CurrentId();
            if (!Guid.TryParse(User.FindFirstValue("sid"), out var sid) ||
                !await db.AuthSessions.AnyAsync(s => s.Id == sid && s.UserId == uid && s.CreatedAt > DateTime.UtcNow.AddMinutes(-5)
                    && s.RevokedAt == null && s.ExpiresAt > DateTime.UtcNow, ct))
                throw new AuthException("REAUTHENTICATION_REQUIRED", 401, "Đăng nhập lại trước khi liên kết.");
            user = await db.Users.SingleAsync(u => u.Id == uid, ct);
            db.ExternalLogins.Add(new ExternalLogin { UserId = user.Id, Provider = h.Provider, ProviderSubject = h.ProviderSubject });
        }
        else
        {
            var candidate = h.PendingEmail ?? (h.ProviderEmail == null ? null : AuthSecurity.NormalizeEmail(h.ProviderEmail));
            if (candidate != null && await db.Users.AnyAsync(u => u.NormalizedEmail == candidate, ct))
                throw new AuthException("ACCOUNT_LINK_REQUIRED", 409, "Đăng nhập tài khoản hiện có rồi chủ động liên kết.");
            if (h.PendingEmail == null || string.IsNullOrEmpty(request.EmailVerificationToken) ||
                h.EmailTokenHash != AuthSecurity.Hash(request.EmailVerificationToken))
                throw new AuthException("EMAIL_VERIFICATION_REQUIRED", 403, "Bổ sung và xác minh email để tạo tài khoản.");
            user = new User { Email = h.PendingEmail, NormalizedEmail = h.PendingEmail, FullName = h.FullName,
                EmailVerified = true, PasswordHash = null, Role = UserRole.Customer,
                AuthProvider = h.Provider == "google" ? AuthProvider.Google : AuthProvider.Facebook };
            db.Users.Add(user);
            db.ExternalLogins.Add(new ExternalLogin { UserId = user.Id, Provider = h.Provider, ProviderSubject = h.ProviderSubject });
        }
        h.ConsumedAt = DateTime.UtcNow;
        h.EmailTokenHash = null;
        h.Version = Guid.NewGuid();
        var result = await auth.CreateSessionAsync(user, ct);
        browser.Write(Response, result);
        Response.Cookies.Delete(TransactionCookie, BrowserSession.Cookie());
        return Ok(new { success = true, data = result, message = (string?)null });
    }

    private Guid CurrentId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) && User.Identity?.IsAuthenticated == true
        ? id : throw new AuthException("REAUTHENTICATION_REQUIRED", 401, "Yêu cầu đăng nhập tài khoản hiện có.");
}

public sealed record ExternalExchange([Required, StringLength(128, MinimumLength = 32)] string Code,
    [property: StringLength(128)] string? EmailVerificationToken = null, bool LinkExistingAccount = false);
public sealed record OnboardingEmail([Required, StringLength(128, MinimumLength = 32)] string Code,
    [Required, EmailAddress, StringLength(254)] string Email, [Required, StringLength(150)] string FullName);
