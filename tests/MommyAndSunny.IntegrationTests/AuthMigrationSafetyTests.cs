using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using MommyAndSunny.Infrastructure.Persistence;

namespace MommyAndSunny.IntegrationTests;

public class AuthMigrationSafetyTests
{
    [Fact]
    public void MigrationSql_AuditsBeforeChanges_AndBackfillsBeforeUniqueIndex()
    {
        using var db = new DesignTimeDbContextFactory().CreateDbContext(Array.Empty<string>());
        // Offline SQL generation only: this factory deliberately has an unreachable connection target.
        var sql = db.GetService<IMigrator>().GenerateScript("20261022131113_AddChatProductTags", "20261026040155_HardenMemberAuthentication");
        Assert.True(sql.IndexOf("Normalized email conflicts exist", StringComparison.Ordinal) < sql.IndexOf("ADD \"NormalizedEmail\"", StringComparison.Ordinal));
        Assert.True(sql.IndexOf("UPDATE \"Users\" SET \"NormalizedEmail\"", StringComparison.Ordinal) < sql.IndexOf("CREATE UNIQUE INDEX \"IX_Users_NormalizedEmail\"", StringComparison.Ordinal));
        Assert.DoesNotContain("DELETE FROM \"Users\"", sql);
        Assert.DoesNotContain("UPDATE \"Orders\"", sql);
        Assert.Contains("IX_ExternalLogins_Provider_ProviderSubject", sql);
    }

    [Fact]
    public async Task AdminPolicy_RequiresServerIssuedAdminRole()
    {
        var services = new ServiceCollection(); services.AddLogging(); services.AddAuthorization();
        using var provider = services.BuildServiceProvider(); var auth = provider.GetRequiredService<IAuthorizationService>();
        var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().RequireRole("Admin").Build();
        var customer = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Customer") }, "Bearer"));
        var admin = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Admin") }, "Bearer"));
        Assert.False((await auth.AuthorizeAsync(customer, null, policy)).Succeeded);
        Assert.True((await auth.AuthorizeAsync(admin, null, policy)).Succeeded);
    }
}
