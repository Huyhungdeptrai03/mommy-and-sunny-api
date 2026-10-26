using Microsoft.AspNetCore.Mvc;
using MommyAndSunny.Application.Auth.DTOs;
using Microsoft.AspNetCore.RateLimiting;
using MommyAndSunny.Application.Auth.Services;
using MommyAndSunny.API.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MommyAndSunny.Application.Common.Interfaces;

namespace MommyAndSunny.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    [EnableRateLimiting("authentication")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly BrowserSession _browser;

        public AuthController(IAuthService authService, BrowserSession browser)
        {
            _authService = authService;
            _browser = browser;
        }

        // Validate RegisterRequest/LoginRequest giờ chạy tự động qua ValidationActionFilter
        // (đăng ký toàn cục trong Program.cs) — không cần inject + gọi IValidator thủ công nữa
        [HttpPost("register")]
        [EnableRateLimiting("sensitive-authentication")]
        public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
        {
            var result = await _authService.RegisterAsync(request, cancellationToken);

            return StatusCode(201, new
            {
                success = true,
                data = result,
                message = "Vui lòng kiểm tra email để xác thực tài khoản"
            });
        }

        [HttpPost("login")]
        [BrowserCsrf]
        public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
        {
            var result = await _authService.LoginAsync(request, cancellationToken);
            _browser.Write(Response, result);

            return Ok(new
            {
                success = true,
                data = result,
                message = (string?)null
            });
        }

        [HttpPost("refresh-token"), EnableRateLimiting(MemberAuthRateLimits.Refresh)]
        [BrowserCsrf]
        public async Task<IActionResult> RefreshToken(CancellationToken cancellationToken)
        {
            var raw = Request.Cookies[BrowserSession.RefreshCookie];
            if (string.IsNullOrEmpty(raw)) throw new MommyAndSunny.Domain.Exceptions.AuthException("SESSION_EXPIRED", 401, "Yêu cầu đăng nhập.");
            AuthResponse result;
            try { result = await _authService.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = raw }, cancellationToken); }
            catch (MommyAndSunny.Domain.Exceptions.AuthException) { _browser.Clear(Response); throw; }
            _browser.Write(Response, result);
            return Ok(new { success = true, data = result, message = (string?)null });
        }

        [HttpGet("csrf"), EnableRateLimiting(MemberAuthRateLimits.Csrf)]
        public IActionResult Csrf([FromServices] IAntiforgery antiforgery)
        {
            Response.Headers.CacheControl = "no-store";
            return Ok(new { success = true, data = new { csrfToken = antiforgery.GetAndStoreTokens(HttpContext).RequestToken }, message = (string?)null });
        }

        [HttpPost("logout"), BrowserCsrf, EnableRateLimiting(MemberAuthRateLimits.Logout)]
        public async Task<IActionResult> Logout(CancellationToken ct)
        {
            await _authService.LogoutAsync(Request.Cookies[BrowserSession.RefreshCookie], ct);
            _browser.Clear(Response);
            return Ok(new { success = true, data = (object?)null, message = (string?)null });
        }

        [HttpGet("me"), Authorize, EnableRateLimiting(MemberAuthRateLimits.Read)]
        public async Task<IActionResult> Me([FromServices] IApplicationDbContext db, [FromServices] ICurrentUserService current, CancellationToken ct)
        {
            Response.Headers.CacheControl = "no-store";
            var user = await db.Users.AsNoTracking().SingleAsync(u => u.Id == current.UserId, ct);
            return Ok(new { success = true, data = AuthService.Map(user), message = (string?)null });
        }

        [HttpPost("resend-verification"), EnableRateLimiting("sensitive-authentication")]
        public async Task<IActionResult> Resend(ForgotPasswordRequest request, CancellationToken ct)
        {
            await _authService.ResendVerificationAsync(request.Email, ct);
            return Accepted(new { success = true, data = (object?)null, message = "Nếu tài khoản cần xác thực, hướng dẫn sẽ được gửi qua email." });
        }

        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail(VerifyEmailRequest request, CancellationToken cancellationToken)
        {
            await _authService.VerifyEmailAsync(request, cancellationToken);
            return Ok(new { success = true, data = (object?)null, message = "Xác thực email thành công" });
        }

        [HttpPost("forgot-password")]
        [EnableRateLimiting("sensitive-authentication")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken)
        {
            await _authService.ForgotPasswordAsync(request, cancellationToken);
            // Luôn trả về cùng 1 message dù email có tồn tại hay không — khớp với logic Backend
            return Ok(new
            {
                success = true,
                data = (object?)null,
                message = "Nếu email này đã đăng ký, chúng tôi đã gửi hướng dẫn đặt lại mật khẩu"
            });
        }

        [HttpPost("reset-password")]
        [EnableRateLimiting("sensitive-authentication")]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken)
        {
            await _authService.ResetPasswordAsync(request, cancellationToken);
            return Ok(new { success = true, data = (object?)null, message = "Đặt lại mật khẩu thành công" });
        }
    }
}

