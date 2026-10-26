using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using MommyAndSunny.API.Services;
namespace MommyAndSunny.API.Hubs;

public sealed class HubConnections
{
    public ConcurrentDictionary<string, HubCallerContext> Active { get; } = new();
}

public sealed class SessionHubFilter(SessionValidationService sessions, HubConnections connections) : IHubFilter
{
    public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext context, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        if (!await sessions.IsValidAsync(context.Context.User, context.Context.ConnectionAborted))
        {
            context.Context.Abort();
            throw new HubException("SESSION_REVOKED");
        }
        return await next(context);
    }
    public async Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    {
        connections.Active[context.Context.ConnectionId] = context.Context;
        try { await next(context); }
        catch { connections.Active.TryRemove(context.Context.ConnectionId, out _); throw; }
    }
    public async Task OnDisconnectedAsync(HubLifetimeContext context, Exception? exception, Func<HubLifetimeContext, Exception?, Task> next)
    {
        connections.Active.TryRemove(context.Context.ConnectionId, out _);
        await next(context, exception);
    }
}

// Group broadcasts do not invoke hub methods: also check idle connections.
public sealed class HubSessionRevocationWorker(IServiceScopeFactory scopes, HubConnections connections,
    ILogger<HubSessionRevocationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            foreach (var connection in connections.Active.Values)
            {
                try
                {
                    using var scope = scopes.CreateScope();
                    var validator = scope.ServiceProvider.GetRequiredService<SessionValidationService>();
                    if (!await validator.IsValidAsync(connection.User, stoppingToken)) connection.Abort();
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
                catch (Exception)
                {
                    connection.Abort();
                    logger.LogWarning("Chat session validation unavailable; connection closed.");
                }
            }
        }
    }
}
