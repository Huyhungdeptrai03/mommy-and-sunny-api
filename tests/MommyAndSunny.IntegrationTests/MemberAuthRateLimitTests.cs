using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using MommyAndSunny.API.Controllers;
using MommyAndSunny.API.Services;

namespace MommyAndSunny.IntegrationTests;

// Real ASP.NET rate-limiting middleware, no web host / DB / provider network.
public class MemberAuthRateLimitTests
{
    private sealed class Pipeline : IDisposable
    {
        private readonly ServiceProvider services;
        private readonly RequestDelegate app;
        public Pipeline()
        {
            var collection = new ServiceCollection();
            collection.AddLogging(); collection.AddMetrics(); collection.AddRouting();
            collection.AddRateLimiter(options => {
                MemberAuthRateLimits.AddPolicies(options);
                options.OnRejected = MemberAuthRateLimits.RejectAsync;
            });
            services = collection.BuildServiceProvider();
            var builder = new ApplicationBuilder(services);
            builder.UseRateLimiter();
            builder.Run(context => { context.Response.StatusCode = 200; return Task.CompletedTask; });
            app = builder.Build();
        }
        public async Task<DefaultHttpContext> Request(string policy, string? user = null)
        {
            var context = new DefaultHttpContext { RequestServices = services };
            context.Connection.RemoteIpAddress = IPAddress.Loopback;
            context.Response.Body = new MemoryStream();
            if (user != null) context.User = new ClaimsPrincipal(new ClaimsIdentity(new[] {
                new Claim(ClaimTypes.NameIdentifier, user) }, "TestBearer"));
            context.SetEndpoint(new Endpoint(_ => Task.CompletedTask,
                new EndpointMetadataCollection(new EnableRateLimitingAttribute(policy)), policy));
            await app(context);
            return context;
        }
        public void Dispose() => services.Dispose();
    }

    [Fact]
    public async Task ThreeTabsSessionTrafficDoesNotConsumeCredentialBudget()
    {
        using var pipeline = new Pipeline();
        for (var tab = 0; tab < 3; tab++) {
            Assert.Equal(200, (await pipeline.Request(MemberAuthRateLimits.Csrf)).Response.StatusCode);
            Assert.Equal(200, (await pipeline.Request(MemberAuthRateLimits.Refresh)).Response.StatusCode);
        }
        for (var round = 0; round < 5; round++)
            for (var tab = 0; tab < 3; tab++)
                Assert.Equal(200, (await pipeline.Request(MemberAuthRateLimits.Read, "member-a")).Response.StatusCode);
        for (var attempt = 0; attempt < 20; attempt++)
            Assert.Equal(200, (await pipeline.Request(MemberAuthRateLimits.Credentials)).Response.StatusCode);
        var rejected = await pipeline.Request(MemberAuthRateLimits.Credentials);
        Assert.Equal(429, rejected.Response.StatusCode);
        Assert.InRange(int.Parse(rejected.Response.Headers.RetryAfter!), 1, 300);
    }

    [Fact]
    public async Task CredentialSensitiveAndOAuthLimitsRemainBoundedAndIndependent()
    {
        using var pipeline = new Pipeline();
        foreach (var policy in new[] { MemberAuthRateLimits.Sensitive, MemberAuthRateLimits.OAuthStart, MemberAuthRateLimits.OAuthEmail }) {
            for (var attempt = 0; attempt < 5; attempt++) Assert.Equal(200, (await pipeline.Request(policy)).Response.StatusCode);
            Assert.Equal(429, (await pipeline.Request(policy)).Response.StatusCode);
        }
        Assert.Equal(200, (await pipeline.Request(MemberAuthRateLimits.OAuth)).Response.StatusCode);
        Assert.Equal(200, (await pipeline.Request(MemberAuthRateLimits.Refresh)).Response.StatusCode);
        Assert.Equal(200, (await pipeline.Request(MemberAuthRateLimits.Logout)).Response.StatusCode);
    }

    [Fact]
    public async Task AuthenticatedReadQuotaIsPerValidatedMemberNotSharedNat()
    {
        using var pipeline = new Pipeline();
        for (var request = 0; request < 20; request++) Assert.Equal(200, (await pipeline.Request(MemberAuthRateLimits.Read, "member-a")).Response.StatusCode);
        Assert.Equal(429, (await pipeline.Request(MemberAuthRateLimits.Read, "member-a")).Response.StatusCode);
        Assert.Equal(200, (await pipeline.Request(MemberAuthRateLimits.Read, "member-b")).Response.StatusCode);
        Assert.Equal(200, (await pipeline.Request(MemberAuthRateLimits.Read)).Response.StatusCode);
    }

    [Fact]
    public async Task RefreshBudgetSupportsThreeTabsAtMinimumJwtTtlButStillRejectsAbuse()
    {
        using var pipeline = new Pipeline();
        for (var request = 0; request < 40; request++) Assert.Equal(200, (await pipeline.Request(MemberAuthRateLimits.Refresh)).Response.StatusCode);
        Assert.Equal(429, (await pipeline.Request(MemberAuthRateLimits.Refresh)).Response.StatusCode);
        Assert.Equal(200, (await pipeline.Request(MemberAuthRateLimits.Credentials)).Response.StatusCode);
    }

    [Fact]
    public void EndpointMetadataSelectsDedicatedPolicies()
    {
        static string? Policy(Type controller, string method) => controller.GetMethod(method)!
            .GetCustomAttributes(typeof(EnableRateLimitingAttribute), true).Cast<EnableRateLimitingAttribute>().SingleOrDefault()?.PolicyName;
        Assert.Equal(MemberAuthRateLimits.Csrf, Policy(typeof(AuthController), "Csrf"));
        Assert.Equal(MemberAuthRateLimits.Refresh, Policy(typeof(AuthController), "RefreshToken"));
        Assert.Equal(MemberAuthRateLimits.Read, Policy(typeof(AuthController), "Me"));
        Assert.Equal(MemberAuthRateLimits.Logout, Policy(typeof(AuthController), "Logout"));
        Assert.Equal(MemberAuthRateLimits.OAuthStart, Policy(typeof(ExternalAuthController), "Start"));
        Assert.Equal(MemberAuthRateLimits.OAuthEmail, Policy(typeof(ExternalAuthController), "SendOnboardingEmail"));
    }
}
