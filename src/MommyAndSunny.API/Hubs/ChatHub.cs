using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MommyAndSunny.Application.Chat.DTOs;
using MommyAndSunny.Application.Chat.Services;
using System.Security.Claims;

namespace MommyAndSunny.API.Hubs;

[Authorize]
public sealed class ChatHub : Hub
{
    private readonly IChatService _chatService;
    private readonly IValidator<SendChatMessageRequest> _messageValidator;

    public ChatHub(IChatService chatService, IValidator<SendChatMessageRequest> messageValidator)
    {
        _chatService = chatService;
        _messageValidator = messageValidator;
    }

    public override async Task OnConnectedAsync()
    {
        if (Context.User?.IsInRole("Admin") == true)
            await Groups.AddToGroupAsync(Context.ConnectionId, "chat-admins");
        await base.OnConnectedAsync();
    }

    public async Task JoinConversation(Guid conversationId)
    {
        var userId = GetUserId();
        if (!await _chatService.CanAccessAsync(conversationId, userId, Context.ConnectionAborted))
            throw new HubException("Bạn không có quyền truy cập cuộc trò chuyện này");
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(conversationId));
    }

    public async Task SendMessage(Guid conversationId, SendChatMessageRequest request)
    {
        var validation = await _messageValidator.ValidateAsync(request, Context.ConnectionAborted);
        if (!validation.IsValid) throw new HubException(validation.Errors[0].ErrorMessage);
        var message = await _chatService.SendMessageAsync(
            conversationId, GetUserId(), request, Context.ConnectionAborted);
        await Clients.Group(GroupName(conversationId)).SendAsync("ReceiveMessage", message, Context.ConnectionAborted);
        await Clients.Group("chat-admins").SendAsync("ConversationUpdated", conversationId, Context.ConnectionAborted);
    }

    public async Task MarkAsRead(Guid conversationId)
    {
        var userId = GetUserId();
        var count = await _chatService.MarkAsReadAsync(conversationId, userId, Context.ConnectionAborted);
        if (count > 0)
            await Clients.Group(GroupName(conversationId)).SendAsync("MessagesRead", conversationId, userId,
                Context.ConnectionAborted);
    }

    public static string GroupName(Guid conversationId) => $"chat:{conversationId:N}";

    private Guid GetUserId()
    {
        var value = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new HubException("Phiên đăng nhập không hợp lệ");
    }
}
