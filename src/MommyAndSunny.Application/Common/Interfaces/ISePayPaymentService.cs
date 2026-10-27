using MommyAndSunny.Application.Common.Payments.DTOs;
namespace MommyAndSunny.Application.Common.Interfaces;

public interface ISePayPaymentService
{
    bool IsEnabled { get; }
    Task<SePayCreatePaymentResult> CreatePaymentRequestAsync(
        string orderCode, decimal amount, string orderInfo, CancellationToken cancellationToken = default);
    bool VerifyWebhookAuthorization(string? authorization);
    bool IsReceivingAccount(string accountNumber);
}
