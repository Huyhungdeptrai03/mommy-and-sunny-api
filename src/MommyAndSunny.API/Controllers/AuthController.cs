using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using MommyAndSunny.Application.Auth.DTOs;
using MommyAndSunny.Application.Auth.Services;

namespace MommyAndSunny.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IValidator<RegisterRequest> _registerValidator;
        private readonly IValidator<LoginRequest> _loginValidator;

        public AuthController(
            IAuthService authService,
            IValidator<RegisterRequest> registerValidator,
            IValidator<LoginRequest> loginValidator)
        {
            _authService = authService;
            _registerValidator = registerValidator;
            _loginValidator = loginValidator;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
        {
            var validationResult = await _registerValidator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    data = (object?)null,
                    message = "Dữ liệu không hợp lệ",
                    errors = validationResult.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
                });
            }

            var result = await _authService.RegisterAsync(request, cancellationToken);

            return StatusCode(201, new
            {
                success = true,
                data = result,
                message = "Vui lòng kiểm tra email để xác thực tài khoản"
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
        {
            var validationResult = await _loginValidator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    data = (object?)null,
                    message = "Dữ liệu không hợp lệ",
                    errors = validationResult.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
                });
            }

            var result = await _authService.LoginAsync(request, cancellationToken);

            return Ok(new
            {
                success = true,
                data = result,
                message = (string?)null
            });
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken(RefreshTokenRequest request, CancellationToken cancellationToken)
        {
            var result = await _authService.RefreshTokenAsync(request, cancellationToken);
            return Ok(new { success = true, data = result, message = (string?)null });
        }

        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail(VerifyEmailRequest request, CancellationToken cancellationToken)
        {
            await _authService.VerifyEmailAsync(request, cancellationToken);
            return Ok(new { success = true, data = (object?)null, message = "Xác thực email thành công" });
        }

        [HttpPost("forgot-password")]
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
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken)
        {
            await _authService.ResetPasswordAsync(request, cancellationToken);
            return Ok(new { success = true, data = (object?)null, message = "Đặt lại mật khẩu thành công" });
        }
    }
}

