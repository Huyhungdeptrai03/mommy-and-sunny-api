using Microsoft.EntityFrameworkCore;
using MommyAndSunny.Application.Auth.DTOs;
using MommyAndSunny.Application.Auth.Services;
using MommyAndSunny.Application.Common.Interfaces;
using MommyAndSunny.Domain.Entities;
using MommyAndSunny.Domain.Exceptions;
using MommyAndSunny.Infrastructure.Identity;
using MommyAndSunny.Infrastructure.Persistence;
using Npgsql;

namespace MommyAndSunny.IntegrationTests;

public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MOMMY_AUTH_TEST_POSTGRES")))
            Skip = "Requires explicitly configured isolated loopback PostgreSQL database (MOMMY_AUTH_TEST_POSTGRES).";
    }
}

public class PostgresMemberAuthTests
{
    // Never uses application configuration, never starts the API, never executes migrations.
    private sealed class IsolatedDatabase : IAsyncDisposable
    {
        private readonly string schema = "auth_test_" + Guid.NewGuid().ToString("N");
        private string connection = "";
        public ApplicationDbContext Context() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connection).Options);
        public static async Task<IsolatedDatabase> Create()
        {
            var fixture = new IsolatedDatabase();
            var cs = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("MOMMY_AUTH_TEST_POSTGRES"));
            if (cs.Host is not ("127.0.0.1" or "localhost" or "::1") || cs.Database?.StartsWith("mommy_auth_test_", StringComparison.Ordinal) != true)
                throw new InvalidOperationException("Tests require loopback host and database named mommy_auth_test_*. Refusing connection.");
            cs.SearchPath = fixture.schema; cs.IncludeErrorDetail = false;
            fixture.connection = cs.ConnectionString;
            await using var connection = new NpgsqlConnection(fixture.connection); await connection.OpenAsync();
            await using (var command = new NpgsqlCommand($"CREATE SCHEMA \"{fixture.schema}\"", connection)) await command.ExecuteNonQueryAsync();
            await using var db = fixture.Context();
            await db.Database.ExecuteSqlRawAsync(db.Database.GenerateCreateScript());
            return fixture;
        }
        public async ValueTask DisposeAsync()
        {
            // schema is generated above, never read from configuration/user input.
            if (!System.Text.RegularExpressions.Regex.IsMatch(schema, "^auth_test_[a-f0-9]{32}$")) throw new InvalidOperationException();
            await using var connection = new NpgsqlConnection(this.connection); await connection.OpenAsync();
            await using var command = new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE", connection); await command.ExecuteNonQueryAsync();
        }
    }

    [PostgresFact]
    public async Task ParallelRefresh_OnlyOneSuccessor_Commits_AndFamilyIsRevoked()
    {
        await using var fixture = await IsolatedDatabase.Create();
        string refresh;
        await using (var setup = fixture.Context())
        {
            var u = new User { Email = "parallel@test.local" }; setup.Users.Add(u); await setup.SaveChangesAsync();
            refresh = (await MemberAuthTests.Auth(setup).CreateSessionAsync(u)).RefreshToken;
        }
        using var barrier = new Barrier(2);
        async Task<string> Rotate()
        {
            await using var db = fixture.Context();
            try { await MemberAuthTests.Auth(db, jwt: new BarrierJwt(barrier)).RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = refresh }); return "OK"; }
            catch (AuthException ex) { return ex.Code; }
        }
        var results = await Task.WhenAll(Task.Run(Rotate), Task.Run(Rotate));
        Assert.Single(results, x => x == "OK"); Assert.Single(results, x => x == "REFRESH_CONFLICT");
        await using var verify = fixture.Context();
        Assert.Equal(2, await verify.RefreshTokens.CountAsync());
        Assert.NotNull((await verify.AuthSessions.SingleAsync()).RevokedAt);
    }

    [PostgresFact]
    public async Task ReplayAndLogout_InvalidatePreviouslyIssuedAccessToken()
    {
        await using var fixture = await IsolatedDatabase.Create(); await using var db = fixture.Context();
        var user = new User { Email = "logout@test.local" }; db.Users.Add(user); await db.SaveChangesAsync();
        var service = MemberAuthTests.Auth(db); var first = await service.CreateSessionAsync(user);
        var session = await db.AuthSessions.SingleAsync(); var principal = MemberAuthTests.Principal(user, session.Id);
        var next = await service.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = first.RefreshToken });
        Assert.Equal("REFRESH_CONFLICT", (await Assert.ThrowsAsync<AuthException>(() => service.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = first.RefreshToken }))).Code);
        db.ChangeTracker.Clear();
        Assert.False(await new MommyAndSunny.API.Services.SessionValidationService(db).IsValidAsync(principal));
        Assert.Equal("SESSION_REVOKED", (await Assert.ThrowsAsync<AuthException>(() => service.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = next.RefreshToken }))).Code);
        var login = await service.CreateSessionAsync(await db.Users.SingleAsync());
        var loginSession = await db.AuthSessions.SingleAsync(s => s.Id != session.Id);
        var loginPrincipal = MemberAuthTests.Principal(user, loginSession.Id);
        var tokenCount = await db.RefreshTokens.CountAsync();
        await service.LogoutAsync(login.RefreshToken);
        // ExecuteUpdate intentionally bypasses the tracked entities in this request.
        // Production uses scoped DbContext/AuthService: a subsequent HTTP request
        // must read the committed revocation, not this test's stale tracked session.
        await using var followUpRequest = fixture.Context();
        Assert.NotNull((await followUpRequest.AuthSessions.AsNoTracking().SingleAsync(s => s.Id == loginSession.Id)).RevokedAt);
        Assert.False(await new MommyAndSunny.API.Services.SessionValidationService(followUpRequest).IsValidAsync(loginPrincipal));
        Assert.Equal("SESSION_REVOKED", (await Assert.ThrowsAsync<AuthException>(() => MemberAuthTests.Auth(followUpRequest)
            .RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = login.RefreshToken }))).Code);
        Assert.Equal(tokenCount, await followUpRequest.RefreshTokens.CountAsync());
    }

    [PostgresFact]
    public async Task ConcurrentHandoffExchange_CreatesOnlyOneSession()
    {
        await using var fixture = await IsolatedDatabase.Create(); var code = AuthSecurity.RandomToken(); var binding = AuthSecurity.RandomToken();
        await using (var db = fixture.Context())
        {
            var u = new User { Email = "handoff@test.local" }; db.Users.Add(u);
            db.ExternalLogins.Add(new ExternalLogin { UserId = u.Id, Provider = "google", ProviderSubject = "stable" });
            db.ExternalHandoffs.Add(new ExternalHandoff { CodeHash = AuthSecurity.Hash(code), BrowserHash = AuthSecurity.Hash(binding),
                Provider = "google", ProviderSubject = "stable", ExpiresAt = DateTime.UtcNow.AddMinutes(5) }); await db.SaveChangesAsync();
        }
        using var barrier = new Barrier(2);
        async Task<string> Exchange()
        {
            await using var db = fixture.Context(); var auth = MemberAuthTests.Auth(db, jwt: new BarrierJwt(barrier));
            var controller = new MommyAndSunny.API.Controllers.ExternalAuthController(db, auth,
                new MommyAndSunny.API.Services.BrowserSession(MemberAuthTests.Config, null!), new MemberAuthTests.CapturingEmail(), MemberAuthTests.Config, null!);
            controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext { HttpContext = MemberAuthTests.Http() };
            controller.Request.Headers.Cookie = MommyAndSunny.API.Controllers.ExternalAuthController.TransactionCookie + "=" + binding;
            try { await controller.Exchange(new MommyAndSunny.API.Controllers.ExternalExchange(code), default); return "OK"; }
            catch (AuthException ex) { return ex.Code; }
        }
        var results = await Task.WhenAll(Task.Run(Exchange), Task.Run(Exchange));
        Assert.Single(results, x => x == "OK"); Assert.Single(results, x => x == "REFRESH_CONFLICT");
        await using var verify = fixture.Context(); Assert.Single(await verify.AuthSessions.ToListAsync());
    }

    [PostgresFact]
    public async Task UniqueExternalIdentity_AndUserConcurrency_AreEnforcedByPostgres()
    {
        await using var fixture = await IsolatedDatabase.Create(); Guid id;
        await using (var setup = fixture.Context())
        {
            var user = new User { Email = "unique@test.local" }; setup.Users.Add(user); await setup.SaveChangesAsync(); id = user.Id;
            setup.ExternalLogins.Add(new ExternalLogin { UserId = id, Provider = "google", ProviderSubject = "unique" }); await setup.SaveChangesAsync();
        }
        await using var a = fixture.Context(); await using var b = fixture.Context();
        var ua = await a.Users.SingleAsync(); var ub = await b.Users.SingleAsync();
        ua.EmailVerified = true; ub.EmailVerified = true; await a.SaveChangesAsync();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => b.SaveChangesAsync());
        await using var duplicate = fixture.Context(); duplicate.ExternalLogins.Add(new ExternalLogin { UserId = id, Provider = "google", ProviderSubject = "unique" });
        await Assert.ThrowsAsync<DbUpdateException>(() => duplicate.SaveChangesAsync());
    }

    private sealed class BarrierJwt(Barrier barrier) : IJwtTokenGenerator
    {
        private readonly JwtTokenGenerator inner = new(MemberAuthTests.Config);
        public string GenerateAccessToken(User user, Guid sid)
        {
            if (!barrier.SignalAndWait(TimeSpan.FromSeconds(20))) throw new TimeoutException("Concurrency rendezvous failed.");
            return inner.GenerateAccessToken(user, sid);
        }
        public string GenerateRefreshToken() => inner.GenerateRefreshToken();
    }
}
