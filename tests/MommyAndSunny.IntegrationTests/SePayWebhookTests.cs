using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MommyAndSunny.API.Controllers;
using MommyAndSunny.Application.Common.Interfaces;
using MommyAndSunny.Application.Common.Payments.DTOs;
using MommyAndSunny.Domain.Entities;
using MommyAndSunny.Domain.Enums;
using MommyAndSunny.Infrastructure.Persistence;

namespace MommyAndSunny.IntegrationTests;

public class SePayWebhookTests
{
    [Theory]
    [InlineData(false, "Apikey secret", 404)]
    [InlineData(true, "Apikey wrong", 400)]
    [InlineData(true, "Apikey secret", 200)]
    public async Task RealGatewayEnforcesAvailabilityAndAuthentication(bool enabled, string authorization, int status)
    {
        await using var context = CreateContext();
        var order = AddSePayOrder(context);
        var historical = new Payment { OrderId = order.Id, Provider = PaymentMethod.SePay,
            TransactionId = "historical", Amount = 1, Status = PaymentTransactionStatus.Success };
        context.Payments.Add(historical);
        await context.SaveChangesAsync();
        var gateway = new MommyAndSunny.Infrastructure.ExternalServices.SePayPaymentGateway(
            Microsoft.Extensions.Options.Options.Create(new MommyAndSunny.Application.Common.Options.SePayOptions {
                Enabled = enabled, Bank = "test", AccountNumber = "001234", WebhookApiKey = "secret" }));
        var controller = new PaymentsController(context, gateway, NullLogger<PaymentsController>.Instance) {
            ControllerContext = new ControllerContext { HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext() } };
        controller.Request.Headers.Authorization = authorization;
        var result = await controller.HandleWebhook(CreateRequest(order), CancellationToken.None);
        Assert.Equal(status, Assert.IsAssignableFrom<Microsoft.AspNetCore.Mvc.Infrastructure.IStatusCodeActionResult>(result).StatusCode);
        Assert.Equal(status == 200 ? OrderPaymentStatus.Paid : OrderPaymentStatus.Unpaid, order.PaymentStatus);
        Assert.Equal(status == 200 ? 2 : 1, await context.Payments.CountAsync());
        Assert.Equal("historical", (await context.Payments.FindAsync(historical.Id))!.TransactionId);
        Assert.Single(context.Orders);
    }

    [Fact]
    public async Task ValidIpn_MarksOrderPaidAndStoresTransaction()
    {
        await using var context = CreateContext();
        var order = AddSePayOrder(context);
        var controller = CreateController(context);

        var result = await controller.HandleWebhook(CreateRequest(order), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(OrderPaymentStatus.Paid, order.PaymentStatus);
        var payment = await context.Payments.SingleAsync();
        Assert.Equal("123456", payment.TransactionId);
        Assert.Equal(order.TotalAmount, payment.Amount);
    }

    [Fact]
    public async Task WrongAmount_DoesNotMarkOrderPaid()
    {
        await using var context = CreateContext();
        var order = AddSePayOrder(context);
        var controller = CreateController(context);
        var request = CreateRequest(order);
        request.TransferAmount++;

        var result = await controller.HandleWebhook(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(OrderPaymentStatus.Unpaid, order.PaymentStatus);
        Assert.Empty(context.Payments);
    }

    [Fact]
    public async Task DuplicateIpn_IsIdempotent()
    {
        await using var context = CreateContext();
        var order = AddSePayOrder(context);
        var controller = CreateController(context);
        var request = CreateRequest(order);

        var first = await controller.HandleWebhook(request, CancellationToken.None);
        var second = await controller.HandleWebhook(request, CancellationToken.None);

        Assert.IsType<OkObjectResult>(first);
        Assert.IsType<OkObjectResult>(second);
        Assert.Equal(1, await context.Payments.CountAsync());
    }

    [Theory]
    [InlineData("out", "001234", false)]
    [InlineData("in", "wrong-account", false)]
    [InlineData("in", "001234", true)]
    public async Task InvalidTransfer_DoesNotMarkPaid(string type, string account, bool cancelled)
    {
        await using var context = CreateContext();
        var order = AddSePayOrder(context);
        if (cancelled) order.Status = OrderStatus.Cancelled;
        var request = CreateRequest(order);
        request.TransferType = type;
        request.AccountNumber = account;
        await CreateController(context).HandleWebhook(request, CancellationToken.None);
        Assert.Equal(OrderPaymentStatus.Unpaid, order.PaymentStatus);
        Assert.Empty(context.Payments);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static Order AddSePayOrder(ApplicationDbContext context)
    {
        var order = new Order
        {
            OrderCode = "MS001",
            UserId = Guid.NewGuid(),
            RecipientName = "Test",
            RecipientPhone = "0900000000",
            ShippingAddress = "Test address",
            PaymentMethod = PaymentMethod.SePay,
            PaymentStatus = OrderPaymentStatus.Unpaid,
            Status = OrderStatus.Pending,
            TotalAmount = 100000
        };
        context.Orders.Add(order);
        context.SaveChanges();
        return order;
    }

    private static PaymentsController CreateController(IApplicationDbContext context)
    {
        var controller = new PaymentsController(context, new AcceptAllSePayWebhooks(), NullLogger<PaymentsController>.Instance);
        controller.ControllerContext = new ControllerContext { HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext() };
        return controller;
    }

    private static SePayWebhookRequest CreateRequest(Order order) => new()
    {
        Code = order.OrderCode, AccountNumber = "001234", TransferType = "in",
        TransferAmount = order.TotalAmount, Id = 123456
    };

    private sealed class AcceptAllSePayWebhooks : ISePayPaymentService
    {
        public bool IsEnabled => true;
        public Task<SePayCreatePaymentResult> CreatePaymentRequestAsync(
            string orderCode, decimal amount, string orderInfo, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public bool VerifyWebhookAuthorization(string? authorization) => true;
        public bool IsReceivingAccount(string accountNumber) => accountNumber == "001234";
    }
}
