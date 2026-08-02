using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Common.Interfaces
{
    public interface IEmailService
    {
        Task SendVerificationEmailAsync(
            string toEmail,
            string fullName,
            string verificationLink,
            CancellationToken cancellationToken = default);

        Task SendPasswordResetEmailAsync(
            string toEmail,
            string fullName,
            string resetLink,
            CancellationToken cancellationToken = default);
    }

}
