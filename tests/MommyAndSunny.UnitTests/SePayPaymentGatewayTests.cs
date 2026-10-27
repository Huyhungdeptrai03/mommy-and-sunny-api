using Microsoft.Extensions.Options;
using MommyAndSunny.Application.Common.Options;
using MommyAndSunny.Infrastructure.ExternalServices;
namespace MommyAndSunny.UnitTests;

public class SePayPaymentGatewayTests
{
    private static SePayPaymentGateway Gateway(string key = "secret") => new(Options.Create(new SePayOptions
    { Enabled = true, Bank = "Vietcombank", AccountNumber = "001234", WebhookApiKey = key }));

    [Fact]
    public async Task OmittedEnabledDefaultsToDisabledEvenWithCredentials()
    {
        var gateway = new SePayPaymentGateway(Options.Create(new SePayOptions {
            Bank = "Vietcombank", AccountNumber = "001234", WebhookApiKey = "secret" }));
        Assert.False(gateway.IsEnabled);
        var result = await gateway.CreatePaymentRequestAsync("MS001", 100000, "");
        Assert.False(result.Success);
        Assert.Null(result.PayUrl);
        Assert.False(gateway.VerifyWebhookAuthorization("Apikey secret"));
    }

    [Theory]
    [InlineData("Apikey secret", true)]
    [InlineData("apikey secret", true)]
    [InlineData("Bearer secret", false)]
    [InlineData("Apikey wrong", false)]
    [InlineData(null, false)]
    public void AuthenticatesWebhook(string? header, bool valid) =>
        Assert.Equal(valid, Gateway().VerifyWebhookAuthorization(header));

    [Fact]
    public void EmptySecretFailsClosed() => Assert.False(Gateway("").VerifyWebhookAuthorization("Apikey "));

    [Fact]
    public async Task DisabledEvenWithCredentialsRejectsQrAndWebhook()
    {
        var gateway = new SePayPaymentGateway(Options.Create(new SePayOptions {
            Enabled = false, Bank = "Vietcombank", AccountNumber = "001234", WebhookApiKey = "secret" }));
        Assert.False(gateway.IsEnabled);
        Assert.False((await gateway.CreatePaymentRequestAsync("MS001", 100000, "")).Success);
        Assert.False(gateway.VerifyWebhookAuthorization("Apikey secret"));
        Assert.False(gateway.IsReceivingAccount("001234"));
    }

    [Fact]
    public async Task MissingWebhookSecretDoesNotIssuePaymentQr()
    {
        var result = await Gateway("").CreatePaymentRequestAsync("MS001", 100000, "");
        Assert.False(result.Success);
        Assert.Null(result.PayUrl);
        Assert.Contains("SePay:WebhookApiKey", result.ErrorMessage);
    }

    [Fact]
    public async Task QrContainsExactAmountAndOrderCode()
    {
        var result = await Gateway().CreatePaymentRequestAsync("MS001", 100000, "ignored");
        Assert.True(result.Success);
        Assert.Equal("https://vietqr.app/img?acc=001234&bank=Vietcombank&amount=100000&des=MS001", result.PayUrl);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1.5)]
    public async Task RejectsInvalidVndAmount(decimal amount) =>
        Assert.False((await Gateway().CreatePaymentRequestAsync("MS001", amount, "")).Success);
}
