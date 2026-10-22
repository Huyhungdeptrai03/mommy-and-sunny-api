using Microsoft.EntityFrameworkCore;
using MommyAndSunny.Application.Chat.DTOs;
using MommyAndSunny.Application.Chat.Services;
using MommyAndSunny.Domain.Entities;
using MommyAndSunny.Domain.Enums;
using MommyAndSunny.Domain.Exceptions;
using MommyAndSunny.Infrastructure.Persistence;

namespace MommyAndSunny.IntegrationTests;

public class ChatServiceTests
{
    [Fact]
    public async Task ProductTags_AreMatchedAndPreservedWhenProductChanges()
    {
        await using var context = CreateContext();
        var customer = AddUser(context, UserRole.Customer, "tags@test.local");
        var product = new Product { Name = "Sữa cho bé", Slug = "sua-cho-be", Status = ProductStatus.Published };
        context.Products.AddRange(product,
            new Product { Name = "Sữa", Slug = "sua", Status = ProductStatus.Published },
            new Product { Name = "Hàng nháp", Slug = "nhap", Status = ProductStatus.Draft });
        await context.SaveChangesAsync();
        var service = new ChatService(context);
        var conversation = await service.GetOrCreateForCustomerAsync(customer.Id);
        var message = await service.SendMessageAsync(conversation.Id, customer.Id,
            new SendChatMessageRequest { Content = "  Muốn #SỮA CHO BÉ, thêm #Sữa cho bé! #Hàng nháp #không có abc#Sữa #Sữax  " });

        Assert.Equal(2, message.ProductTags.Count);
        Assert.All(message.ProductTags, tag =>
        {
            Assert.Equal(product.Id, tag.ProductId);
            Assert.Equal("Sữa cho bé", tag.ProductName);
            Assert.Equal("sua-cho-be", tag.Slug);
            Assert.Equal("#Sữa cho bé", message.Content!.Substring(tag.Start, tag.Length), ignoreCase: true);
        });

        product.Name = "Tên mới";
        product.Status = ProductStatus.Discontinued;
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var history = await service.GetMessagesAsync(conversation.Id, customer.Id, new ChatMessageFilterRequest());
        Assert.Equal(2, history.Items.Single().ProductTags.Count);
        Assert.Equal("Sữa cho bé", history.Items.Single().ProductTags[0].ProductName);
    }

    [Theory]
    [InlineData("Tin nhắn bình thường", null)]
    [InlineData(null, "https://example.com/photo.jpg")]
    [InlineData("#không tồn tại", null)]
    public async Task MessagesWithoutMatchingProducts_HaveNoTags(string? content, string? imageUrl)
    {
        await using var context = CreateContext();
        var customer = AddUser(context, UserRole.Customer, "plain@test.local");
        await context.SaveChangesAsync();
        var service = new ChatService(context);
        var conversation = await service.GetOrCreateForCustomerAsync(customer.Id);
        var message = await service.SendMessageAsync(conversation.Id, customer.Id,
            new SendChatMessageRequest { Content = content, ImageUrl = imageUrl });
        Assert.Empty(message.ProductTags);
    }

    [Fact]
    public async Task CustomerAndAdmin_CanCompleteConversationFlow()
    {
        await using var context = CreateContext();
        var customer = AddUser(context, UserRole.Customer, "customer@test.local");
        var admin = AddUser(context, UserRole.Admin, "admin@test.local");
        await context.SaveChangesAsync();
        var service = new ChatService(context);

        var conversation = await service.GetOrCreateForCustomerAsync(customer.Id);
        var customerMessage = await service.SendMessageAsync(conversation.Id, customer.Id,
            new SendChatMessageRequest { Content = "Xin chào shop" });
        var assigned = await service.AssignToMeAsync(conversation.Id, admin.Id);
        var adminMessage = await service.SendMessageAsync(conversation.Id, admin.Id,
            new SendChatMessageRequest { Content = "Shop có thể giúp gì cho bạn?" });
        var readCount = await service.MarkAsReadAsync(conversation.Id, customer.Id);

        Assert.Equal("Active", assigned.Status);
        Assert.Equal(customer.Id, customerMessage.SenderId);
        Assert.Equal(admin.Id, adminMessage.SenderId);
        Assert.Equal(1, readCount);
        Assert.Equal(2, await context.ChatMessages.CountAsync());
    }

    [Fact]
    public async Task Customer_CannotReadAnotherCustomersConversation()
    {
        await using var context = CreateContext();
        var owner = AddUser(context, UserRole.Customer, "owner@test.local");
        var attacker = AddUser(context, UserRole.Customer, "attacker@test.local");
        await context.SaveChangesAsync();
        var service = new ChatService(context);
        var conversation = await service.GetOrCreateForCustomerAsync(owner.Id);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            service.GetConversationAsync(conversation.Id, attacker.Id));
    }

    [Fact]
    public async Task ClosedConversation_AllowsCustomerToStartNewConversation()
    {
        await using var context = CreateContext();
        var customer = AddUser(context, UserRole.Customer, "customer@test.local");
        var admin = AddUser(context, UserRole.Admin, "admin@test.local");
        await context.SaveChangesAsync();
        var service = new ChatService(context);
        var first = await service.GetOrCreateForCustomerAsync(customer.Id);
        await service.CloseAsync(first.Id, admin.Id);

        var second = await service.GetOrCreateForCustomerAsync(customer.Id);

        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal("Waiting", second.Status);
    }

    private static ApplicationDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static User AddUser(ApplicationDbContext context, UserRole role, string email)
    {
        var user = new User { Email = email, FullName = email, Role = role, EmailVerified = true };
        context.Users.Add(user);
        return user;
    }
}
