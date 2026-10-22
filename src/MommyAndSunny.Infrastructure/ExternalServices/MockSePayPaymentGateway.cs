using System.Globalization;
using System.Net;
using System.Text;
using MommyAndSunny.Application.Common.Interfaces;
using MommyAndSunny.Application.Common.Payments.DTOs;

namespace MommyAndSunny.Infrastructure.ExternalServices;

// Registered only in Development. No bank or external SePay calls are made.
public sealed class MockSePayPaymentGateway : ISePayPaymentService
{
    public const string ApiKey = "sepay-local-test";
    public const string AccountNumber = "SEPAY_TEST_ACCOUNT";

    public Task<SePayCreatePaymentResult> CreatePaymentRequestAsync(
        string orderCode, decimal amount, string orderInfo, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(orderCode) || amount <= 0 || amount != decimal.Truncate(amount))
            return Task.FromResult(new SePayCreatePaymentResult { Success = false, ErrorMessage = "Invalid test payment" });

        // A local placeholder image intentionally contains no scannable bank QR.
        var svg = $"<svg xmlns='http://www.w3.org/2000/svg' width='640' height='240'><rect width='100%' height='100%' fill='#fef3c7'/><g font-family='sans-serif' font-size='20' fill='#111827'><text x='24' y='45'>SEPAY LOCAL TEST - NO MONEY TRANSFER</text><text x='24' y='95'>{WebUtility.HtmlEncode(orderCode)}</text><text x='24' y='140'>{amount.ToString("0", CultureInfo.InvariantCulture)} VND</text><text x='24' y='195'>Send a test webhook to confirm payment.</text></g></svg>";
        return Task.FromResult(new SePayCreatePaymentResult
        {
            Success = true,
            PayUrl = "data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(svg))
        });
    }

    public bool VerifyWebhookAuthorization(string? authorization) =>
        string.Equals(authorization, "Apikey " + ApiKey, StringComparison.Ordinal);
    public bool IsReceivingAccount(string accountNumber) => accountNumber == AccountNumber;
}
