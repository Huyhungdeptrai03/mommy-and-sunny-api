using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MommyAndSunny.Application.Auth.DTOs;
using MommyAndSunny.Application.Common.Interfaces;
using MommyAndSunny.Domain.Entities;
using MommyAndSunny.Domain.Enums;
using MommyAndSunny.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace MommyAndSunny.Application.Auth.Services
{
    public class AuthService : IAuthService
    {
        private readonly IApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public AuthService(
            IApplicationDbContext context,
            IPasswordHasher passwordHasher,
            IJwtTokenGenerator jwtTokenGenerator,
            IEmailService emailService,
            IConfiguration configuration)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
            _emailService = emailService;
            _configuration = configuration;
        }

        public async Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
        {
            var emailExists = await _context.Users
                .AnyAsync(u => u.Email == request.Email, cancellationToken);

            if (emailExists)
            {
                throw new ConflictException("Email đã được sử dụng");
            }

            var user = new User
            {
                Email = request.Email,
                PasswordHash = _passwordHasher.Hash(request.Password),
                FullName = request.FullName,
                PhoneNumber = request.PhoneNumber,
                AuthProvider = AuthProvider.Local,
                Role = UserRole.Customer,
                EmailVerified = false
            };

            // Sinh sẵn token xác thực NGAY LÚC TẠO — tránh phải query lại user sau khi save
            var verificationToken = GenerateRandomToken();
            user.EmailVerificationTokenHash = HashToken(verificationToken);
            user.EmailVerificationTokenExpiresAt = DateTime.UtcNow.AddHours(24);

            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);

            var frontendBaseUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:3000";
            var verificationLink = $"{frontendBaseUrl}/verify-email?token={verificationToken}";

            await _emailService.SendVerificationEmailAsync(
                user.Email, user.FullName, verificationLink, cancellationToken);

            return new RegisterResponse { UserId = user.Id, Email = user.Email };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

            // Gộp chung 1 thông báo lỗi cho cả 2 trường hợp (sai email / sai mật khẩu)
            // để tránh lộ thông tin tài khoản nào đã tồn tại — theo đúng nghiệp vụ đã thống nhất
            if (user is null || user.PasswordHash is null ||
                !_passwordHasher.Verify(request.Password, user.PasswordHash))
            {
                throw new UnauthorizedException("Sai email hoặc mật khẩu");
            }

            var accessToken = _jwtTokenGenerator.GenerateAccessToken(user);
            var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();

            _context.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                TokenHash = HashToken(refreshToken),
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });
            await _context.SaveChangesAsync(cancellationToken);

            return new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                User = MapToUserDto(user)
            };
        }

        public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
        {
            var tokenHash = HashToken(request.RefreshToken);

            var storedToken = await _context.RefreshTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

            if (storedToken is null || !storedToken.IsActive)
            {
                throw new UnauthorizedException("Refresh token không hợp lệ hoặc đã hết hạn");
            }

            // Rotation: thu hồi token cũ ngay khi dùng, phát hành token mới
            storedToken.RevokedAt = DateTime.UtcNow;

            var newAccessToken = _jwtTokenGenerator.GenerateAccessToken(storedToken.User);
            var newRefreshToken = _jwtTokenGenerator.GenerateRefreshToken();

            _context.RefreshTokens.Add(new RefreshToken
            {
                UserId = storedToken.UserId,
                TokenHash = HashToken(newRefreshToken),
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });

            await _context.SaveChangesAsync(cancellationToken);

            return new AuthResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                User = MapToUserDto(storedToken.User)
            };
        }

        public async Task VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken = default)
        {
            var tokenHash = HashToken(request.Token);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.EmailVerificationTokenHash == tokenHash, cancellationToken);

            if (user is null
                || user.EmailVerificationTokenExpiresAt is null
                || user.EmailVerificationTokenExpiresAt < DateTime.UtcNow)
            {
                throw new UnauthorizedException("Link xác thực không hợp lệ hoặc đã hết hạn");
            }

            user.EmailVerified = true;
            // Xóa token sau khi dùng — token xác thực chỉ dùng được đúng 1 lần
            user.EmailVerificationTokenHash = null;
            user.EmailVerificationTokenExpiresAt = null;

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

            // KHÔNG tiết lộ email có tồn tại hay không — luôn coi như thành công dù email
            // không có trong hệ thống, tránh kẻ xấu dò danh sách email đã đăng ký
            if (user is null)
            {
                return;
            }

            var resetToken = GenerateRandomToken();
            user.PasswordResetTokenHash = HashToken(resetToken);
            user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddHours(1);

            await _context.SaveChangesAsync(cancellationToken);

            var frontendBaseUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:3000";
            var resetLink = $"{frontendBaseUrl}/reset-password?token={resetToken}";

            await _emailService.SendPasswordResetEmailAsync(user.Email, user.FullName, resetLink, cancellationToken);
        }

        public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
        {
            var tokenHash = HashToken(request.Token);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.PasswordResetTokenHash == tokenHash, cancellationToken);

            if (user is null
                || user.PasswordResetTokenExpiresAt is null
                || user.PasswordResetTokenExpiresAt < DateTime.UtcNow)
            {
                throw new UnauthorizedException("Link đặt lại mật khẩu không hợp lệ hoặc đã hết hạn");
            }

            if (request.NewPassword.Length < 8)
            {
                throw new DomainException("Mật khẩu mới phải có ít nhất 8 ký tự");
            }

            user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
            user.PasswordResetTokenHash = null;
            user.PasswordResetTokenExpiresAt = null;

            // Thu hồi TOÀN BỘ refresh token đang hoạt động của user này — buộc đăng nhập lại
            // trên mọi thiết bị sau khi đổi mật khẩu, đúng thông lệ bảo mật chuẩn (trường hợp
            // mật khẩu cũ đã bị lộ, phiên đăng nhập cũ cũng cần bị vô hiệu theo)
            var activeTokens = await _context.RefreshTokens
                .Where(t => t.UserId == user.Id && t.RevokedAt == null)
                .ToListAsync(cancellationToken);
            foreach (var token in activeTokens)
            {
                token.RevokedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        private static UserDto MapToUserDto(User user) => new()
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role.ToString()
        };

        private static string HashToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(bytes);
        }

        private static string GenerateRandomToken()
        {
            var randomBytes = new byte[32];
            RandomNumberGenerator.Fill(randomBytes);
            // Base64 URL-safe — để nhét thẳng vào query string mà không cần URL-encode
            return Convert.ToBase64String(randomBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }
    }
}


