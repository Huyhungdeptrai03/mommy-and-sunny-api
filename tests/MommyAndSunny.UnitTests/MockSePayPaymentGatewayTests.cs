using MommyAndSunny.Infrastructure.ExternalServices;
namespace MommyAndSunny.UnitTests;

public class MockSePayPaymentGatewayTests
{
    private static MockSePayPaymentGateway Gateway(string origin = "https://shop.test") => new(origin);

    [Fact]
    public async Task CreatesHttpsImageUrlWithoutBankConfiguration()
    {
        var result = await Gateway().CreatePaymentRequestAsync("MS001", 100000, "");
        Assert.True(result.Success);
        Assert.Equal("https://shop.test/api/payments/mock-qr?orderCode=MS001&amount=100000", result.PayUrl);
        Assert.Equal("https", new Uri(result.PayUrl!).Scheme);
        Assert.DoesNotContain("data:", result.PayUrl);
    }

    [Theory]
    [InlineData(0)] [InlineData(-1)] [InlineData(1.5)]
    public async Task InvalidAmountDoesNotIssuePayment(decimal amount) =>
        Assert.False((await Gateway().CreatePaymentRequestAsync("MS001", amount, "")).Success);

    [Theory]
    [InlineData("http://shop.test")] [InlineData("https://user:pass@shop.test")] [InlineData("https://shop.test/path")]
    public async Task InvalidOriginDoesNotIssuePayment(string origin) =>
        Assert.False((await Gateway(origin).CreatePaymentRequestAsync("MS001", 100000, "")).Success);

    [Fact]
    public void OnlyAcceptsExplicitTestCredentials()
    {
        var gateway = Gateway();
        Assert.True(gateway.VerifyWebhookAuthorization("Apikey sepay-local-test"));
        Assert.False(gateway.VerifyWebhookAuthorization(null));
        Assert.False(gateway.VerifyWebhookAuthorization("Apikey real-key"));
        Assert.True(gateway.IsReceivingAccount("SEPAY_TEST_ACCOUNT"));
        Assert.False(gateway.IsReceivingAccount("123456"));
    }
}
