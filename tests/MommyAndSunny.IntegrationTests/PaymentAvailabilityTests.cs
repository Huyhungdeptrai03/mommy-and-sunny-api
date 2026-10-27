using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MommyAndSunny.API.Controllers;
using MommyAndSunny.Application.Common.Interfaces;
using MommyAndSunny.Application.Common.Options;
using MommyAndSunny.Application.Orders.DTOs;
using MommyAndSunny.Application.Orders.Services;
using MommyAndSunny.Domain.Entities;
using MommyAndSunny.Domain.Enums;
using MommyAndSunny.Domain.Exceptions;
using MommyAndSunny.Infrastructure.ExternalServices;
using MommyAndSunny.Infrastructure.Persistence;
using System.Text.Json;

namespace MommyAndSunny.IntegrationTests;

public class PaymentAvailabilityTests
{
    [Theory]
    [InlineData(false, PaymentMethod.COD)]
    [InlineData(false, PaymentMethod.SePay)]
    [InlineData(true, PaymentMethod.COD)]
    [InlineData(true, PaymentMethod.SePay)]
    public async Task CheckoutRespectsAvailabilityWithoutMutatingRejectedCart(bool enabled, PaymentMethod method)
    {
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var user = new User { Email = "fixture@example.invalid", EmailVerified = true };
        var address = new Address { User = user, UserId = user.Id };
        var cart = new Cart { User = user, UserId = user.Id };
        var product = new Product { Name = "Fixture" };
        var variant = new ProductVariant { Product = product, Price = 100000, StockQuantity = 10 };
        var item = new CartItem { Cart = cart, ProductVariant = variant, Quantity = 1 };
        db.AddRange(user, address, cart, product, variant, item);
        await db.SaveChangesAsync();
        var gateway = new SePayPaymentGateway(Options.Create(new SePayOptions {
            Enabled = enabled, Bank = "test", AccountNumber = "001", WebhookApiKey = "fixture" }));
        var orders = new OrderService(db, new Current(user.Id), gateway, null!);
        var request = new CreateOrderRequest { AddressId = address.Id, PaymentMethod = method };
        if (!enabled && method == PaymentMethod.SePay)
        {
            await Assert.ThrowsAsync<DomainException>(() => orders.CreateOrderAsync(request));
            Assert.Empty(db.Orders);
            Assert.Single(db.CartItems);
            Assert.Equal(0, variant.ReservedQuantity);
        }
        else
        {
            await orders.CreateOrderAsync(request);
            Assert.Single(db.Orders);
            Assert.Empty(db.CartItems);
            Assert.Equal(1, variant.ReservedQuantity);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AdvertisesOnlyAvailableMethods(bool enabled)
    {
        var gateway = new SePayPaymentGateway(Options.Create(new SePayOptions {
            Enabled = enabled, Bank = "test", AccountNumber = "001", WebhookApiKey = "fixture" }));
        var controller = new PaymentMethodsController(gateway) {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
        var json = JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(controller.Get()).Value);
        Assert.Contains("COD", json);
        Assert.Equal(enabled, json.Contains("SePay"));
    }

    private sealed record Current(Guid? UserId) : ICurrentUserService;
}
