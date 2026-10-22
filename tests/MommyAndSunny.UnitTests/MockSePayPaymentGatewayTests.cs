using MommyAndSunny.Infrastructure.ExternalServices;
namespace MommyAndSunny.UnitTests;

public class MockSePayPaymentGatewayTests
{
    [Fact]
    public async Task CreatesLocalImageWithoutBankConfiguration()
    {
        var result = await new MockSePayPaymentGateway().CreatePaymentRequestAsync("MS001", 100000, "");
        Assert.True(result.Success);
        Assert.StartsWith("data:image/svg+xml;base64,", result.PayUrl);
    }

    [Fact]
    public void OnlyAcceptsExplicitTestCredentials()
    {
        var gateway = new MockSePayPaymentGateway();
        Assert.True(gateway.VerifyWebhookAuthorization("Apikey sepay-local-test"));
        Assert.False(gateway.VerifyWebhookAuthorization(null));
        Assert.False(gateway.VerifyWebhookAuthorization("Apikey real-key"));
        Assert.True(gateway.IsReceivingAccount("SEPAY_TEST_ACCOUNT"));
        Assert.False(gateway.IsReceivingAccount("123456"));
    }
}
