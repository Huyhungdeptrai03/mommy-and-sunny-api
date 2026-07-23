using Microsoft.EntityFrameworkCore;
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

        public AuthService(
            IApplicationDbContext context,
            IPasswordHasher passwordHasher,
            IJwtTokenGenerator jwtTokenGenerator)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
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

            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);

            // TODO (bước sau): sinh token xác thực + gọi IEmailService gửi mail xác thực

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

            // Lưu HASH của refresh token vào DB — cần thiết để endpoint /auth/refresh-token
            // sau này có thể validate token có thật/còn hiệu lực/chưa bị thu hồi hay không
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
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    Role = user.Role.ToString()
                }
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

            // Rotation: thu hồi token cũ ngay khi dùng, phát hành token mới —
            // nếu 1 refresh token bị đánh cắp và dùng trước người dùng thật,
            // lần dùng tiếp theo của người dùng thật sẽ thất bại vì token đã bị thu hồi,
            // qua đó phát hiện được có dấu hiệu bị đánh cắp
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
                User = new UserDto
                {
                    Id = storedToken.User.Id,
                    Email = storedToken.User.Email,
                    FullName = storedToken.User.FullName,
                    Role = storedToken.User.Role.ToString()
                }
            };
        }

        private static string HashToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(bytes);
        }
    }
}
