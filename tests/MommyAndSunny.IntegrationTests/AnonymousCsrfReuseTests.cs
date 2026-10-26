using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MommyAndSunny.API.Services;
using MommyAndSunny.Domain.Entities;

namespace MommyAndSunny.IntegrationTests;

public class AnonymousCsrfReuseTests
{
    [Fact]
    public async Task CachedAnonymousTokenIsReusableButCannotBeUsedForBearerLink()
    {
        var services = new ServiceCollection(); services.AddLogging();
        services.AddDataProtection().UseEphemeralDataProtectionProvider();
        services.AddAntiforgery(options => { options.HeaderName = "X-CSRF-TOKEN"; options.Cookie.Name = "__Host-ms-csrf"; options.Cookie.SecurePolicy = CookieSecurePolicy.Always; });
        using var provider = services.BuildServiceProvider();
        var af = provider.GetRequiredService<IAntiforgery>();
        var first = MemberAuthTests.Http(); first.RequestServices = provider;
        var tokens = af.GetAndStoreTokens(first);
        var cookie = first.Response.Headers.SetCookie.ToString().Split(';')[0];
        var browser = new BrowserSession(MemberAuthTests.Config, af);
        DefaultHttpContext Post(string? token)
        {
            var context = MemberAuthTests.Http(); context.RequestServices = provider;
            context.Request.Method = "POST"; context.Request.Headers.Cookie = cookie;
            context.Request.Headers.Origin = "https://shop.test";
            context.Request.Headers["X-CSRF-TOKEN"] = token;
            return context;
        }
        Assert.True(await browser.ValidateAsync(Post(tokens.RequestToken)));
        Assert.True(await browser.ValidateAsync(Post(tokens.RequestToken)));
        var user = new User { Email = "csrf@test.local" };
        var bearer = Post(tokens.RequestToken);
        bearer.User = MemberAuthTests.Principal(user, Guid.NewGuid());
        Assert.False(await browser.ValidateAsync(bearer));
        var authenticatedGet = MemberAuthTests.Http(); authenticatedGet.RequestServices = provider;
        authenticatedGet.Request.Headers.Cookie = cookie; authenticatedGet.User = bearer.User;
        var authenticatedToken = af.GetAndStoreTokens(authenticatedGet).RequestToken;
        var authenticatedPost = Post(authenticatedToken); authenticatedPost.User = bearer.User;
        Assert.True(await browser.ValidateAsync(authenticatedPost));
    }
}
