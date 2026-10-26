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
            _logger.LogWarning("Development email sink: message not sent. Recovery secrets are never logged.");

            return Task.CompletedTask;
        }

        public Task SendPasswordResetEmailAsync(
            string toEmail,
            string fullName,
            string resetLink,
            CancellationToken cancellationToken = default)
        {
            _logger.LogWarning("Development email sink: message not sent. Recovery secrets are never logged.");

            return Task.CompletedTask;
        }
    }
}