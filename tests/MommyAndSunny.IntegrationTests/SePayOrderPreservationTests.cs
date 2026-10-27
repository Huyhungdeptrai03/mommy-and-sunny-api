using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MommyAndSunny.Application.Common.Interfaces;
using MommyAndSunny.Domain.Entities;
using MommyAndSunny.Domain.Enums;
using MommyAndSunny.Infrastructure.BackgroundJobs;
using MommyAndSunny.Infrastructure.Persistence;

namespace MommyAndSunny.IntegrationTests;

public class SePayOrderPreservationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CleanupPreservesDisabledSePayOrdersAndPaymentHistory(bool enabled)
    {
        var services = new ServiceCollection();
        // One context for this isolated job invocation and subsequent assertions.
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        services.AddSingleton<IApplicationDbContext>(db);
        using var provider = services.BuildServiceProvider();
        var order = new Order { OrderCode = "OLD", PaymentMethod = PaymentMethod.SePay,
            PaymentStatus = OrderPaymentStatus.Unpaid, Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow.AddDays(-1) };
        var cod = new Order { OrderCode = "COD", PaymentMethod = PaymentMethod.COD,
            Status = OrderStatus.Pending, CreatedAt = DateTime.UtcNow.AddDays(-1) };
        var payment = new Payment { OrderId = order.Id, Provider = PaymentMethod.SePay,
            TransactionId = "old", Amount = 100, Status = PaymentTransactionStatus.Success };
        db.AddRange(order, cod, payment);
        await db.SaveChangesAsync();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["SePay:Enabled"] = enabled.ToString(), ["OrderCleanup:CheckIntervalMinutes"] = "60" }).Build();
        using var job = new StaleOrderCleanupJob(provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<StaleOrderCleanupJob>.Instance, configuration);
        var method = typeof(StaleOrderCleanupJob).GetMethod("CleanupStaleOrdersAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        await (Task)method.Invoke(job, new object[] { CancellationToken.None })!;
        Assert.Equal(enabled ? OrderStatus.Cancelled : OrderStatus.Pending, order.Status);
        Assert.Equal(OrderStatus.Pending, cod.Status);
        Assert.Equal(2, await db.Orders.CountAsync());
        Assert.Equal("old", (await db.Payments.SingleAsync()).TransactionId);
        Assert.Equal(enabled ? 1 : 0, await db.OrderStatusHistories.CountAsync());
    }
}
