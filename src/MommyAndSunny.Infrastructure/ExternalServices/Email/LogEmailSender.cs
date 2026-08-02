using Microsoft.Extensions.Logging;
using MommyAndSunny.Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Infrastructure.ExternalServices.Email
{
    // CỐ Ý đặt tên KHÔNG kết thúc bằng "Service" — để Scrutor (auto DI scan) không
    // tự động bắt nhầm class này (vì không có interface "ILogEmailSender" tương ứng,
    // nếu để tên trùng convention sẽ gây lỗi lúc khởi động ứng dụng). Đăng ký thủ công ở Program.cs.
    public class LogEmailSender : IEmailService
    {
        private readonly ILogger<LogEmailSender> _logger;

        public LogEmailSender(ILogger<LogEmailSender> logger)
        {
            _logger = logger;
        }

        public Task SendVerificationEmailAsync(
            string toEmail,
            string fullName,
            string verificationLink,
            CancellationToken cancellationToken = default)
        {
            _logger.LogWarning(
                "===== [DEV] EMAIL XÁC THỰC (chưa gửi thật) =====\n" +
                "Gửi tới: {Email} ({FullName})\n" +
                "Link xác thực (copy vào trình duyệt để test): {Link}\n" +
                "==================================================",
                toEmail, fullName, verificationLink);

            return Task.CompletedTask;
        }

        public Task SendPasswordResetEmailAsync(
            string toEmail,
            string fullName,
            string resetLink,
            CancellationToken cancellationToken = default)
        {
            _logger.LogWarning(
                "===== [DEV] EMAIL ĐẶT LẠI MẬT KHẨU (chưa gửi thật) =====\n" +
                "Gửi tới: {Email} ({FullName})\n" +
                "Link đặt lại mật khẩu (copy vào trình duyệt để test): {Link}\n" +
                "==========================================================",
                toEmail, fullName, resetLink);

            return Task.CompletedTask;
        }
    }
}