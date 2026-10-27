using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using MommyAndSunny.API.Controllers;

namespace MommyAndSunny.IntegrationTests;

public class MockPaymentImageTests
{
    private sealed class EnvironmentStub(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "test";
        public string ContentRootPath { get; set; } = ".";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
    private static MockPaymentsController Controller(string environment = "Development", bool mock = true, bool? enabled = true)
    {
        var settings = new Dictionary<string, string?> { ["SePay:UseMock"] = mock.ToString() };
        if (enabled.HasValue) settings["SePay:Enabled"] = enabled.Value.ToString();
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        return new(new EnvironmentStub(environment), config) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
    }
    [Fact]
    public void DisabledOrOmittedFlagDoesNotServeMockImage()
    {
        Assert.IsType<NotFoundResult>(Controller(enabled: false).Image("MS001", 100000));
        Assert.IsType<NotFoundResult>(Controller(enabled: null).Image("MS001", 100000));
    }

    [Fact]
    public void DevelopmentServesNonPayableImageAndEscapesInput()
    {
        var controller = Controller();
        var result = Assert.IsType<ContentResult>(controller.Image("MS<script>", 100000));
        Assert.StartsWith("image/svg+xml", result.ContentType);
        Assert.Contains("NO MONEY TRANSFER", result.Content);
        Assert.Contains("MS&lt;script&gt;", result.Content);
        Assert.Contains("100000 VND", result.Content);
        Assert.DoesNotContain("<script>", result.Content);
        Assert.Equal("no-store", controller.Response.Headers.CacheControl);
        Assert.Contains("sandbox", controller.Response.Headers["Content-Security-Policy"].ToString());
    }
    [Theory]
    [InlineData("Production", true)] [InlineData("Staging", true)] [InlineData("Development", false)]
    public void EndpointIsUnavailableOutsideExplicitDevelopmentMock(string environment, bool mock) =>
        Assert.IsType<NotFoundResult>(Controller(environment, mock).Image("MS001", 100000));
    [Theory]
    [InlineData(0)] [InlineData(-1)] [InlineData(1.5)]
    public void InvalidPayloadIsRejected(decimal amount) => Assert.IsType<BadRequestResult>(Controller().Image("MS001", amount));
}
