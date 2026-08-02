using Microsoft.Extensions.Configuration;
using MommyAndSunny.Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace MommyAndSunny.Infrastructure.ExternalServices.Email
{
    public class SmtpEmailSender : IEmailService
    {
        private readonly IConfiguration _configuration;

        public SmtpEmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendVerificationEmailAsync(
            string toEmail,
            string fullName,
            string verificationLink,
            CancellationToken cancellationToken = default)
        {
            var host = _configuration["Smtp:Host"]
                ?? throw new InvalidOperationException("Thiếu cấu hình Smtp:Host");
            var port = int.Parse(_configuration["Smtp:Port"] ?? "587");
            var username = _configuration["Smtp:Username"];
            var password = _configuration["Smtp:Password"];
            var fromEmail = _configuration["Smtp:FromEmail"] ?? username ?? "";
            var fromName = _configuration["Smtp:FromName"] ?? "Mommy and Sunny";

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = true
            };

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = "Xác thực tài khoản Mommy and Sunny",
                Body =
                    $"Chào {fullName},<br/><br/>" +
                    $"Vui lòng bấm vào link sau để xác thực tài khoản: " +
                    $"<a href=\"{verificationLink}\">{verificationLink}</a><br/><br/>" +
                    $"Link có hiệu lực trong 24 giờ.",
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            await client.SendMailAsync(message, cancellationToken);
        }

        public async Task SendPasswordResetEmailAsync(
            string toEmail,
            string fullName,
            string resetLink,
            CancellationToken cancellationToken = default)
        {
            var host = _configuration["Smtp:Host"]
                ?? throw new InvalidOperationException("Thiếu cấu hình Smtp:Host");
            var port = int.Parse(_configuration["Smtp:Port"] ?? "587");
            var username = _configuration["Smtp:Username"];
            var password = _configuration["Smtp:Password"];
            var fromEmail = _configuration["Smtp:FromEmail"] ?? username ?? "";
            var fromName = _configuration["Smtp:FromName"] ?? "Mommy and Sunny";

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = true
            };

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = "Đặt lại mật khẩu Mommy and Sunny",
                Body =
                    $"Chào {fullName},<br/><br/>" +
                    $"Bạn (hoặc ai đó) vừa yêu cầu đặt lại mật khẩu. Nếu đây không phải bạn, " +
                    $"hãy bỏ qua email này.<br/><br/>" +
                    $"Bấm vào link sau để đặt lại mật khẩu: " +
                    $"<a href=\"{resetLink}\">{resetLink}</a><br/><br/>" +
                    $"Link có hiệu lực trong 1 giờ.",
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            await client.SendMailAsync(message, cancellationToken);
        }
    }

}
