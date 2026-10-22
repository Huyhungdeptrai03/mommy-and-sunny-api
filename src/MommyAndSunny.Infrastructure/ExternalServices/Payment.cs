using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using MommyAndSunny.Application.Common.Interfaces;
using MommyAndSunny.Application.Common.Options;
using MommyAndSunny.Application.Common.Payments.DTOs;

namespace MommyAndSunny.Infrastructure.ExternalServices;

public sealed class SePayPaymentGateway(IOptions<SePayOptions> options) : ISePayPaymentService
{
    private readonly SePayOptions _options = options.Value;

    public Task<SePayCreatePaymentResult> CreatePaymentRequestAsync(
        string orderCode, decimal amount, string orderInfo, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(_options.Bank) || string.IsNullOrWhiteSpace(_options.AccountNumber) ||
            string.IsNullOrWhiteSpace(_options.WebhookApiKey))
            return Task.FromResult(new SePayCreatePaymentResult
            {
                Success = false,
                ErrorMessage = "SePay is not configured. Set SePay:Bank, SePay:AccountNumber and SePay:WebhookApiKey."
            });
        if (amount <= 0 || amount != decimal.Truncate(amount) || string.IsNullOrWhiteSpace(orderCode) ||
            string.IsNullOrWhiteSpace(_options.Bank) || string.IsNullOrWhiteSpace(_options.AccountNumber))
            return Task.FromResult(new SePayCreatePaymentResult { Success = false, ErrorMessage = "Th?ng tin thanh to?n SePay kh?ng h?p l?" });

        var url = "https://vietqr.app/img?acc=" + Uri.EscapeDataString(_options.AccountNumber) +
            "&bank=" + Uri.EscapeDataString(_options.Bank) +
            "&amount=" + amount.ToString("0", CultureInfo.InvariantCulture) +
            "&des=" + Uri.EscapeDataString(orderCode);
        return Task.FromResult(new SePayCreatePaymentResult { Success = true, PayUrl = url });
    }

    public bool VerifyWebhookAuthorization(string? authorization)
    {
        if (string.IsNullOrWhiteSpace(_options.WebhookApiKey) ||
            authorization is null || !authorization.StartsWith("Apikey ", StringComparison.OrdinalIgnoreCase))
            return false;
        var expected = SHA256.HashData(Encoding.UTF8.GetBytes(_options.WebhookApiKey));
        var actual = SHA256.HashData(Encoding.UTF8.GetBytes(authorization[7..]));
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    public bool IsReceivingAccount(string accountNumber) =>
        !string.IsNullOrWhiteSpace(_options.AccountNumber) &&
        string.Equals(accountNumber, _options.AccountNumber, StringComparison.Ordinal);
}
