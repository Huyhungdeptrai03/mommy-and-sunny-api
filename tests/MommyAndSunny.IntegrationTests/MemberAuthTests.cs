using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using MommyAndSunny.API.Controllers;
using MommyAndSunny.API.Services;
using MommyAndSunny.Application.Auth.DTOs;
using MommyAndSunny.Application.Auth.Services;
using MommyAndSunny.Application.Common.Interfaces;
using MommyAndSunny.Application.Admin.Users.Services;
using MommyAndSunny.Application.Admin.Users.DTOs;
using MommyAndSunny.Application.Account.Services;
using MommyAndSunny.Application.Cart.Services;
using MommyAndSunny.Application.Orders.Services;
using MommyAndSunny.Application.Orders.DTOs;
using MommyAndSunny.Domain.Entities;
using MommyAndSunny.Domain.Enums;
using MommyAndSunny.Domain.Exceptions;
using MommyAndSunny.Infrastructure.Identity;
using MommyAndSunny.Infrastructure.Persistence;

namespace MommyAndSunny.IntegrationTests;

public class MemberAuthTests
{
    internal static IConfiguration Config => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
        ["Jwt:Secret"] = "test-only-signing-material-at-least-32-bytes-long", ["Jwt:Issuer"] = "test-issuer",
        ["Jwt:Audience"] = "test-audience", ["Jwt:AccessTokenExpirationMinutes"] = "15", ["Frontend:BaseUrl"] = "https://shop.test"
    }).Build();
    internal static ApplicationDbContext Context() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    internal static AuthService Auth(ApplicationDbContext db, CapturingEmail? email = null, IJwtTokenGenerator? jwt = null) =>
        new(db, new PasswordHasher(), jwt ?? new JwtTokenGenerator(Config), email ?? new CapturingEmail(), Config);
    internal static ClaimsPrincipal Principal(User user, Guid session) => new(new ClaimsIdentity(new[] {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Role, user.Role.ToString()),
        new Claim("sid", session.ToString()), new Claim("auth_ver", user.SecurityVersion.ToString()) }, "Bearer"));
    internal static DefaultHttpContext Http() { var h = new DefaultHttpContext(); h.Request.Scheme = "https"; h.Request.Host = new HostString("shop.test"); h.Response.Body = new MemoryStream(); return h; }

    [Fact]
    public async Task Login_NormalizesEmail_HashesRefresh_AndDoesNotSerializeRefresh()
    {
        await using var db = Context();
        var user = new User { Email = "Member@Example.com", FullName = "Member", PasswordHash = new PasswordHasher().Hash("password-123") };
        db.Users.Add(user); await db.SaveChangesAsync();
        var result = await Auth(db).LoginAsync(new LoginRequest { Email = " member@EXAMPLE.com ", Password = "password-123" });
        Assert.Equal(user.Id, result.User.Id);
        Assert.Equal(AuthSecurity.Hash(result.RefreshToken), (await db.RefreshTokens.SingleAsync()).TokenHash);
        Assert.DoesNotContain(result.RefreshToken, JsonSerializer.Serialize(result));
        Assert.DoesNotContain("RefreshToken", JsonSerializer.Serialize(result));
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken);
        Assert.Equal(user.Id.ToString(), token.Subject);
        Assert.True(token.ValidTo < DateTime.UtcNow.AddMinutes(16));
        Assert.True(await new SessionValidationService(db).IsValidAsync(Principal(user, (await db.AuthSessions.SingleAsync()).Id)));
    }

    [Fact]
    public async Task SocialOnlyUser_CannotPasswordLogin_AndForgotPasswordDoesNotCreatePassword()
    {
        await using var db = Context(); var user = new User { Email = "social@example.com", FullName = "Social" };
        db.Users.Add(user); await db.SaveChangesAsync(); var email = new CapturingEmail();
        var ex = await Assert.ThrowsAsync<AuthException>(() => Auth(db).LoginAsync(new LoginRequest { Email = user.Email, Password = "anything" }));
        Assert.Equal("INVALID_CREDENTIALS", ex.Code);
        await Auth(db, email).ForgotPasswordAsync(new ForgotPasswordRequest { Email = user.Email });
        Assert.Null(email.Link); Assert.Null(user.PasswordHash);
    }

    [Fact]
    public async Task ResetPassword_RevokesSessionAndJwt_AndTokenIsSingleUse()
    {
        await using var db = Context(); var user = new User { Email = "reset@example.com", FullName = "Reset", PasswordHash = new PasswordHasher().Hash("old-password") };
        db.Users.Add(user); await db.SaveChangesAsync(); var mail = new CapturingEmail(); var service = Auth(db, mail);
        await service.LoginAsync(new LoginRequest { Email = user.Email, Password = "old-password" });
        var session = await db.AuthSessions.SingleAsync(); var principal = Principal(user, session.Id);
        await service.ForgotPasswordAsync(new ForgotPasswordRequest { Email = user.Email });
        var raw = Uri.UnescapeDataString(mail.Link!.Split("token=")[1]);
        await service.ResetPasswordAsync(new ResetPasswordRequest { Token = raw, NewPassword = "new-password" });
        Assert.False(await new SessionValidationService(db).IsValidAsync(principal));
        Assert.NotNull(session.RevokedAt);
        await Assert.ThrowsAsync<AuthException>(() => service.ResetPasswordAsync(new ResetPasswordRequest { Token = raw, NewPassword = "another-password" }));
    }

    [Fact]
    public async Task RoleChange_RejectsOldAdminPrincipal_AndNumericUndefinedRole()
    {
        await using var db = Context(); var admin = new User { Email = "admin@test.local", Role = UserRole.Admin };
        db.Users.Add(admin); await db.SaveChangesAsync();
        await Auth(db).CreateSessionAsync(admin); var session = await db.AuthSessions.SingleAsync(); var principal = Principal(admin, session.Id);
        var service = new AdminUserService(db);
        await Assert.ThrowsAsync<DomainException>(() => service.UpdateRoleAsync(admin.Id, Guid.NewGuid(), new UpdateUserRoleRequest { Role = "900" }));
        await service.UpdateRoleAsync(admin.Id, Guid.NewGuid(), new UpdateUserRoleRequest { Role = "Customer" });
        Assert.False(await new SessionValidationService(db).IsValidAsync(principal));
    }

    [Fact]
    public async Task JwtValidation_RejectsWrongSignatureAndAudience()
    {
        await using var db = Context(); var u = new User { Email = "jwt@test.local" }; db.Users.Add(u); await db.SaveChangesAsync();
        var result = await Auth(db).CreateSessionAsync(u);
        var parameters = new TokenValidationParameters { ValidateIssuer = true, ValidIssuer = "test-issuer", ValidateAudience = true,
            ValidAudience = "test-audience", ValidateLifetime = true, ValidateIssuerSigningKey = true,
            ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 }, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Config["Jwt:Secret"]!)) };
        new JwtSecurityTokenHandler().ValidateToken(result.AccessToken, parameters, out _);
        parameters.ValidAudience = "wrong";
        Assert.Throws<SecurityTokenInvalidAudienceException>(() => new JwtSecurityTokenHandler().ValidateToken(result.AccessToken, parameters, out _));
        parameters.ValidAudience = "test-audience";
        parameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(new string('x', 64)));
        Assert.ThrowsAny<SecurityTokenException>(() => new JwtSecurityTokenHandler().ValidateToken(result.AccessToken, parameters, out _));
    }

    [Fact]
    public async Task Csrf_RequiresOriginHttpsAndRealFrameworkToken()
    {
        var services = new ServiceCollection(); services.AddLogging(); services.AddDataProtection().UseEphemeralDataProtectionProvider();
        services.AddAntiforgery(o => { o.HeaderName = "X-CSRF-TOKEN"; o.Cookie.Name = "__Host-ms-csrf"; o.Cookie.SecurePolicy = CookieSecurePolicy.Always; });
        using var provider = services.BuildServiceProvider(); var af = provider.GetRequiredService<IAntiforgery>();
        var first = Http(); first.RequestServices = provider;
        var tokens = af.GetAndStoreTokens(first);
        var second = Http(); second.RequestServices = provider; second.Request.Method = "POST";
        second.Request.Headers.Cookie = first.Response.Headers.SetCookie.ToString().Split(';')[0];
        second.Request.Headers["X-CSRF-TOKEN"] = tokens.RequestToken;
        var browser = new BrowserSession(Config, af);
        Assert.False(await browser.ValidateAsync(second));
        second.Request.Headers.Origin = "https://evil.test"; Assert.False(await browser.ValidateAsync(second));
        second.Request.Headers.Origin = "https://shop.test"; Assert.True(await browser.ValidateAsync(second));
        second.Request.Scheme = "http"; Assert.False(await browser.ValidateAsync(second));
        second.Request.Scheme = "https"; second.Request.Headers.Remove("X-CSRF-TOKEN"); Assert.False(await browser.ValidateAsync(second));
    }

    [Fact]
    public void Cookie_IsHostOnlySecureHttpOnly_AndRefreshNeverSerialized()
    {
        var cookie = BrowserSession.Cookie(); Assert.True(cookie.Secure); Assert.True(cookie.HttpOnly);
        Assert.Equal("/", cookie.Path); Assert.Null(cookie.Domain); Assert.Equal(SameSiteMode.Lax, cookie.SameSite);
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<UpdateProfile>("{\"FullName\":\"X\",\"PhoneNumber\":null,\"Role\":\"Admin\"}"));
        Assert.False(AuthSecurity.ValidPassword(new string('é', 37)));
    }

    [Fact]
    public async Task ExternalHandoff_RequiresBrowser_AndReplayIsRejected()
    {
        await using var db = Context(); var u = new User { Email = "linked@test.local" }; db.Users.Add(u);
        db.ExternalLogins.Add(new ExternalLogin { UserId = u.Id, Provider = "google", ProviderSubject = "subject" });
        var code = AuthSecurity.RandomToken(); var binding = AuthSecurity.RandomToken();
        db.ExternalHandoffs.Add(new ExternalHandoff { CodeHash = AuthSecurity.Hash(code), BrowserHash = AuthSecurity.Hash(binding),
            Provider = "google", ProviderSubject = "subject", ExpiresAt = DateTime.UtcNow.AddMinutes(5) }); await db.SaveChangesAsync();
        var controller = External(db); controller.HttpContext.Request.Headers.Cookie = ExternalAuthController.TransactionCookie + "=wrong";
        Assert.Equal("INVALID_HANDOFF", (await Assert.ThrowsAsync<AuthException>(() => controller.Exchange(new ExternalExchange(code), default))).Code);
        controller.HttpContext.Request.Headers.Cookie = ExternalAuthController.TransactionCookie + "=" + binding;
        Assert.IsType<OkObjectResult>(await controller.Exchange(new ExternalExchange(code), default));
        await Assert.ThrowsAsync<AuthException>(() => controller.Exchange(new ExternalExchange(code), default));
        Assert.Single(db.Users); Assert.Equal(u.Id, (await db.AuthSessions.SingleAsync()).UserId);
    }

    [Fact]
    public async Task MatchingEmail_DoesNotMerge_ExplicitRecentAuthenticationCanLink()
    {
        await using var db = Context(); var u = new User { Email = "existing@test.local" }; db.Users.Add(u); await db.SaveChangesAsync();
        await Auth(db).CreateSessionAsync(u); var session = await db.AuthSessions.SingleAsync();
        var code = AuthSecurity.RandomToken(); var binding = AuthSecurity.RandomToken();
        db.ExternalHandoffs.Add(new ExternalHandoff { CodeHash = AuthSecurity.Hash(code), BrowserHash = AuthSecurity.Hash(binding),
            Provider = "facebook", ProviderSubject = "facebook-sub", ProviderEmail = u.Email, ExpiresAt = DateTime.UtcNow.AddMinutes(5) });
        await db.SaveChangesAsync(); var controller = External(db); controller.HttpContext.Request.Headers.Cookie = ExternalAuthController.TransactionCookie + "=" + binding;
        var ex = await Assert.ThrowsAsync<AuthException>(() => controller.Exchange(new ExternalExchange(code), default));
        Assert.Equal("ACCOUNT_LINK_REQUIRED", ex.Code); Assert.Empty(db.ExternalLogins);
        controller.HttpContext.User = Principal(u, session.Id);
        await controller.Exchange(new ExternalExchange(code, LinkExistingAccount: true), default);
        Assert.Equal(u.Id, (await db.ExternalLogins.SingleAsync()).UserId); Assert.Single(db.Users);
    }

    [Fact]
    public async Task MissingProviderEmail_RequiresVerificationBeforeCreatingSocialOnlyUser()
    {
        await using var db = Context(); var code = AuthSecurity.RandomToken(); var binding = AuthSecurity.RandomToken();
        db.ExternalHandoffs.Add(new ExternalHandoff { CodeHash = AuthSecurity.Hash(code), BrowserHash = AuthSecurity.Hash(binding),
            Provider = "facebook", ProviderSubject = "no-email", ExpiresAt = DateTime.UtcNow.AddMinutes(5) }); await db.SaveChangesAsync();
        var mail = new CapturingEmail(); var controller = External(db, mail); controller.HttpContext.Request.Headers.Cookie = ExternalAuthController.TransactionCookie + "=" + binding;
        Assert.Equal("EMAIL_VERIFICATION_REQUIRED", (await Assert.ThrowsAsync<AuthException>(() => controller.Exchange(new ExternalExchange(code), default))).Code);
        Assert.Empty(db.Users);
        await controller.SendOnboardingEmail(new OnboardingEmail(code, "verified@test.local", "Verified"), default);
        var raw = Uri.UnescapeDataString(mail.Link!.Split("emailToken=")[1]);
        await controller.Exchange(new ExternalExchange(code, raw), default);
        var u = await db.Users.SingleAsync(); Assert.True(u.EmailVerified); Assert.Null(u.PasswordHash); Assert.Equal(UserRole.Customer, u.Role);
    }

    [Fact]
    public async Task MemberIsolation_OrdersAddressesCartAndUnverifiedCheckout()
    {
        await using var db = Context(); var a = new User { Email = "a@test.local" }; var b = new User { Email = "b@test.local" };
        db.Users.AddRange(a, b); db.Addresses.Add(new Address { UserId = b.Id, RecipientName = "B" });
        db.Orders.Add(new Order { UserId = b.Id, OrderCode = "B-ORDER" }); await db.SaveChangesAsync();
        var current = new Current(a.Id);
        Assert.Empty(await new AddressService(db, current).GetMyAddressesAsync());
        var cart = await new CartService(db, current).GetCartAsync(); Assert.Empty(cart.Items);
        Assert.Equal(a.Id, (await db.Carts.SingleAsync()).UserId);
        var orders = new OrderService(db, current, null!, null!);
        await Assert.ThrowsAsync<NotFoundException>(() => orders.GetOrderByCodeAsync("B-ORDER"));
        var ex = await Assert.ThrowsAsync<AuthException>(() => orders.CreateOrderAsync(new CreateOrderRequest { PaymentMethod = PaymentMethod.COD }));
        Assert.Equal("EMAIL_VERIFICATION_REQUIRED", ex.Code);
    }

    internal static ExternalAuthController External(ApplicationDbContext db, CapturingEmail? mail = null)
    {
        var controller = new ExternalAuthController(db, Auth(db), new BrowserSession(Config, null!), mail ?? new CapturingEmail(), Config, null!);
        controller.ControllerContext = new ControllerContext { HttpContext = Http() }; return controller;
    }
    private sealed record Current(Guid? UserId) : ICurrentUserService;
    internal sealed class CapturingEmail : IEmailService
    {
        public string? Link { get; private set; }
        public Task SendVerificationEmailAsync(string toEmail, string fullName, string verificationLink, CancellationToken cancellationToken = default) { Link = verificationLink; return Task.CompletedTask; }
        public Task SendPasswordResetEmailAsync(string toEmail, string fullName, string resetLink, CancellationToken cancellationToken = default) { Link = resetLink; return Task.CompletedTask; }
    }
}
