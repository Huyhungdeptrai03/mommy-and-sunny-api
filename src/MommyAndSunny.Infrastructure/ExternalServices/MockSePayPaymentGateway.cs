using System.Globalization;
using System.Net;
using MommyAndSunny.Application.Common.Interfaces;
using MommyAndSunny.Application.Common.Payments.DTOs;

namespace MommyAndSunny.Infrastructure.ExternalServices;

// Development only. Same HTTPS-image URL contract as production; no bank QR.
public sealed class MockSePayPaymentGateway(string publicOrigin) : ISePayPaymentService
{
    public bool IsEnabled => true;
    public const string ApiKey = "sepay-local-test";
    public const string AccountNumber = "SEPAY_TEST_ACCOUNT";

    public Task<SePayCreatePaymentResult> CreatePaymentRequestAsync(
        string orderCode, decimal amount, string orderInfo, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(orderCode) || orderCode.Length > 128 || amount <= 0 || amount != decimal.Truncate(amount))
            return Task.FromResult(new SePayCreatePaymentResult { Success = false, ErrorMessage = "Invalid test payment" });
        if (!Uri.TryCreate(publicOrigin, UriKind.Absolute, out var origin) ||
            origin.Scheme != "https" || !string.IsNullOrEmpty(origin.UserInfo) || origin.AbsolutePath != "/" ||
            !string.IsNullOrEmpty(origin.Query) || !string.IsNullOrEmpty(origin.Fragment))
            return Task.FromResult(new SePayCreatePaymentResult { Success = false, ErrorMessage = "Mock payments require the configured HTTPS public origin" });
        return Task.FromResult(new SePayCreatePaymentResult {
            Success = true,
            PayUrl = origin.GetLeftPart(UriPartial.Authority) + "/api/payments/mock-qr?orderCode=" + Uri.EscapeDataString(orderCode) +
                "&amount=" + amount.ToString("0", CultureInfo.InvariantCulture)
        });
    }

    public static string RenderPlaceholder(string orderCode, decimal amount) =>
        $"<svg xmlns='http://www.w3.org/2000/svg' width='640' height='240'><rect width='100%' height='100%' fill='#fef3c7'/><g font-family='sans-serif' font-size='20' fill='#111827'><text x='24' y='45'>SEPAY LOCAL TEST - NO MONEY TRANSFER</text><text x='24' y='95'>{WebUtility.HtmlEncode(orderCode)}</text><text x='24' y='140'>{amount.ToString("0", CultureInfo.InvariantCulture)} VND</text><text x='24' y='195'>Send a test webhook to confirm payment.</text></g></svg>";

    public bool VerifyWebhookAuthorization(string? authorization) =>
        string.Equals(authorization, "Apikey " + ApiKey, StringComparison.Ordinal);
    public bool IsReceivingAccount(string accountNumber) => accountNumber == AccountNumber;
}
