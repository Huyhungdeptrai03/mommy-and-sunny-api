using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MommyAndSunny.Application.Auth.DTOs;
using MommyAndSunny.Application.Common.Interfaces;
using MommyAndSunny.Domain.Entities;
using MommyAndSunny.Domain.Enums;
using MommyAndSunny.Domain.Exceptions;
namespace MommyAndSunny.Application.Auth.Services;

public class AuthService(IApplicationDbContext db, IPasswordHasher passwords,
    IJwtTokenGenerator jwt, IEmailService email, IConfiguration config) : IAuthService
{
    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        ValidatePassword(request.Password);
        var normalized = AuthSecurity.NormalizeEmail(request.Email);
        if (await db.Users.AnyAsync(u => u.NormalizedEmail == normalized, cancellationToken))
            throw new ConflictException("Email đã được sử dụng");
        var token = AuthSecurity.RandomToken();
        var user = new User {
            Email = request.Email.Trim(), NormalizedEmail = normalized,
            PasswordHash = passwords.Hash(request.Password), FullName = request.FullName.Trim(),
            PhoneNumber = request.PhoneNumber, Role = UserRole.Customer, AuthProvider = AuthProvider.Local,
            EmailVerificationTokenHash = AuthSecurity.Hash(token), EmailVerificationTokenExpiresAt = DateTime.UtcNow.AddHours(24)
        };
        db.Users.Add(user);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException) { throw new ConflictException("Không thể tạo tài khoản; vui lòng thử lại."); }
        await email.SendVerificationEmailAsync(user.Email, user.FullName, Link("verify-email", token), cancellationToken);
        return new RegisterResponse { UserId = user.Id, Email = user.Email };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var normalized = AuthSecurity.NormalizeEmail(request.Email);
        var user = await db.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalized, cancellationToken);
        if (user?.PasswordHash is null || System.Text.Encoding.UTF8.GetByteCount(request.Password) > 72 ||
            !passwords.Verify(request.Password, user.PasswordHash))
            throw new AuthException("INVALID_CREDENTIALS", 401, "Sai email hoặc mật khẩu");
        return await CreateSessionAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> CreateSessionAsync(User user, CancellationToken cancellationToken = default)
    {
        var session = new AuthSession { UserId = user.Id, SecurityVersion = user.SecurityVersion, ExpiresAt = DateTime.UtcNow.AddDays(7) };
        db.AuthSessions.Add(session);
        var result = Issue(user, session);
        // OAuth handoff consumption and new identities commit in this same transaction.
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { throw new AuthException("REFRESH_CONFLICT", 409, "Giao dịch đã thay đổi; vui lòng đăng nhập lại."); }
        catch (DbUpdateException) { throw new AuthException("IDENTITY_CONFLICT", 409, "Danh tính đã được sử dụng; vui lòng đăng nhập lại."); }
        return result;
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var hash = AuthSecurity.Hash(request.RefreshToken);
        var token = await db.RefreshTokens.Include(t => t.User).Include(t => t.Session)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        if (token?.Session is null) throw Failure("SESSION_EXPIRED");
        var session = token.Session;
        if (session.RevokedAt != null) throw Failure("SESSION_REVOKED");
        if (session.SecurityVersion != token.User.SecurityVersion)
        {
            await RevokeSessionAsync(session.Id, cancellationToken);
            throw Failure("SESSION_REVOKED");
        }
        if (token.ConsumedAt != null || token.RevokedAt != null)
        {
            await RevokeSessionAsync(session.Id, cancellationToken);
            throw new AuthException("REFRESH_CONFLICT", 409, "Refresh token đã được sử dụng; vui lòng đăng nhập lại.");
        }
        if (token.ExpiresAt <= DateTime.UtcNow || session.ExpiresAt <= DateTime.UtcNow) throw Failure("SESSION_EXPIRED");
        token.ConsumedAt = token.RevokedAt = DateTime.UtcNow;
        session.Version = Guid.NewGuid();
        var result = Issue(token.User, session);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException)
        {
            // Relational SaveChanges rolls back the losing rotation, including its successor.
            await RevokeSessionAsync(session.Id, cancellationToken);
            throw new AuthException("REFRESH_CONFLICT", 409, "Refresh xung đột; vui lòng đăng nhập lại.");
        }
        return result;
    }

    public async Task LogoutAsync(string? refreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(refreshToken)) return;
        var hash = AuthSecurity.Hash(refreshToken);
        var id = await db.RefreshTokens.Where(t => t.TokenHash == hash).Select(t => t.SessionId).FirstOrDefaultAsync(ct);
        if (id.HasValue) await RevokeSessionAsync(id.Value, ct);
    }

    private async Task RevokeSessionAsync(Guid id, CancellationToken ct) =>
        await db.AuthSessions.Where(s => s.Id == id).ExecuteUpdateAsync(s => s
            .SetProperty(x => x.RevokedAt, DateTime.UtcNow).SetProperty(x => x.Version, Guid.NewGuid()), ct);

    private AuthResponse Issue(User user, AuthSession session)
    {
        var raw = jwt.GenerateRefreshToken();
        db.RefreshTokens.Add(new RefreshToken { UserId = user.Id, SessionId = session.Id,
            TokenHash = AuthSecurity.Hash(raw), ExpiresAt = session.ExpiresAt });
        return new AuthResponse { AccessToken = jwt.GenerateAccessToken(user, session.Id),
            AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(AccessMinutes(config)),
            RefreshToken = raw, RefreshTokenExpiresAt = session.ExpiresAt, User = Map(user) };
    }

    public static int AccessMinutes(IConfiguration c) => Math.Clamp(int.TryParse(c["Jwt:AccessTokenExpirationMinutes"], out var n) ? n : 15, 1, 15);
    public static UserDto Map(User u) => new() { Id = u.Id, Email = u.Email, FullName = u.FullName,
        PhoneNumber = u.PhoneNumber, Role = u.Role.ToString(), EmailVerified = u.EmailVerified };
    private static AuthException Failure(string code) => new(code, 401, "Phiên không còn hợp lệ; vui lòng đăng nhập lại.");
    private static void ValidatePassword(string password)
    {
        if (!AuthSecurity.ValidPassword(password)) throw new AuthException("VALIDATION_FAILED", 400,
            "Mật khẩu cần ít nhất 8 ký tự và tối đa 72 byte UTF-8.");
    }

    public async Task VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken = default)
    {
        var hash = AuthSecurity.Hash(request.Token);
        var user = await db.Users.FirstOrDefaultAsync(u => u.EmailVerificationTokenHash == hash, cancellationToken);
        if (user is null || user.EmailVerificationTokenExpiresAt <= DateTime.UtcNow || user.EmailVerificationTokenExpiresAt is null)
            throw new AuthException("INVALID_TOKEN", 400, "Link xác thực không hợp lệ hoặc hết hạn.");
        user.EmailVerified = true;
        user.EmailVerificationTokenHash = null;
        user.EmailVerificationTokenExpiresAt = null;
        await SaveTokenUse(cancellationToken);
    }

    public async Task ResendVerificationAsync(string address, CancellationToken ct = default)
    {
        var normalized = AuthSecurity.NormalizeEmail(address);
        var user = await db.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalized, ct);
        if (user is null || user.EmailVerified) return;
        var raw = AuthSecurity.RandomToken();
        user.EmailVerificationTokenHash = AuthSecurity.Hash(raw);
        user.EmailVerificationTokenExpiresAt = DateTime.UtcNow.AddHours(24);
        await SaveTokenUse(ct);
        await email.SendVerificationEmailAsync(user.Email, user.FullName, Link("verify-email", raw), ct);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var normalized = AuthSecurity.NormalizeEmail(request.Email);
        var user = await db.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalized, cancellationToken);
        if (user?.PasswordHash is null) return;
        var raw = AuthSecurity.RandomToken();
        user.PasswordResetTokenHash = AuthSecurity.Hash(raw);
        user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddHours(1);
        await SaveTokenUse(cancellationToken);
        await email.SendPasswordResetEmailAsync(user.Email, user.FullName, Link("reset-password", raw), cancellationToken);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        ValidatePassword(request.NewPassword);
        var hash = AuthSecurity.Hash(request.Token);
        var user = await db.Users.FirstOrDefaultAsync(u => u.PasswordResetTokenHash == hash, cancellationToken);
        if (user is null || user.PasswordResetTokenExpiresAt is null || user.PasswordResetTokenExpiresAt <= DateTime.UtcNow)
            throw new AuthException("INVALID_TOKEN", 400, "Link đặt lại mật khẩu không hợp lệ hoặc hết hạn.");
        user.PasswordHash = passwords.Hash(request.NewPassword);
        user.PasswordResetTokenHash = null;
        user.PasswordResetTokenExpiresAt = null;
        user.SecurityVersion = Guid.NewGuid();
        var sessions = await db.AuthSessions.Where(s => s.UserId == user.Id && s.RevokedAt == null).ToListAsync(cancellationToken);
        foreach (var s in sessions) { s.RevokedAt = DateTime.UtcNow; s.Version = Guid.NewGuid(); }
        var tokens = await db.RefreshTokens.Where(t => t.UserId == user.Id && t.RevokedAt == null).ToListAsync(cancellationToken);
        foreach (var t in tokens) t.RevokedAt = DateTime.UtcNow;
        await SaveTokenUse(cancellationToken);
    }

    private async Task SaveTokenUse(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new AuthException("INVALID_TOKEN", 409, "Giao dịch đã thay đổi; vui lòng thử lại."); }
    }
    private string Link(string path, string token) => $"{(config["Frontend:BaseUrl"] ?? "https://localhost:3000").TrimEnd('/')}/{path}?token={Uri.EscapeDataString(token)}";
}
