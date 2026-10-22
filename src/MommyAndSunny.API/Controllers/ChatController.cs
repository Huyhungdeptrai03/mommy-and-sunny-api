using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.RateLimiting;
using MommyAndSunny.API.Hubs;
using MommyAndSunny.Application.Chat.DTOs;
using MommyAndSunny.Application.Chat.Services;
using MommyAndSunny.Application.Common.Interfaces;
using MommyAndSunny.Domain.Exceptions;

namespace MommyAndSunny.API.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("chat")]
[Route("api/chat")]
public sealed class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ICurrentUserService _currentUser;
    private readonly IHubContext<ChatHub> _hub;

    public ChatController(IChatService chatService, ICurrentUserService currentUser, IHubContext<ChatHub> hub)
    {
        _chatService = chatService;
        _currentUser = currentUser;
        _hub = hub;
    }

    [HttpPost("conversations/current")]
    public async Task<IActionResult> GetOrCreateCurrent(CancellationToken cancellationToken)
    {
        var conversation = await _chatService.GetOrCreateForCustomerAsync(UserId, cancellationToken);
        await _hub.Clients.Group("chat-admins").SendAsync("ConversationUpdated", conversation.Id, cancellationToken);
        return Ok(new { success = true, data = conversation, message = (string?)null });
    }

    [HttpGet("conversations/{conversationId:guid}")]
    public async Task<IActionResult> GetConversation(Guid conversationId, CancellationToken cancellationToken)
    {
        var conversation = await _chatService.GetConversationAsync(conversationId, UserId, cancellationToken);
        return Ok(new { success = true, data = conversation, message = (string?)null });
    }

    [HttpGet("conversations/{conversationId:guid}/messages")]
    public async Task<IActionResult> GetMessages(
        Guid conversationId, [FromQuery] ChatMessageFilterRequest filter, CancellationToken cancellationToken)
    {
        var messages = await _chatService.GetMessagesAsync(conversationId, UserId, filter, cancellationToken);
        return Ok(new { success = true, data = messages, message = (string?)null });
    }

    [HttpPost("conversations/{conversationId:guid}/messages")]
    public async Task<IActionResult> SendMessage(
        Guid conversationId, SendChatMessageRequest request, CancellationToken cancellationToken)
    {
        var message = await _chatService.SendMessageAsync(conversationId, UserId, request, cancellationToken);
        await _hub.Clients.Group(ChatHub.GroupName(conversationId))
            .SendAsync("ReceiveMessage", message, cancellationToken);
        await _hub.Clients.Group("chat-admins").SendAsync("ConversationUpdated", conversationId, cancellationToken);
        return StatusCode(201, new { success = true, data = message, message = (string?)null });
    }

    [HttpPost("conversations/{conversationId:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid conversationId, CancellationToken cancellationToken)
    {
        var count = await _chatService.MarkAsReadAsync(conversationId, UserId, cancellationToken);
        if (count > 0)
            await _hub.Clients.Group(ChatHub.GroupName(conversationId))
                .SendAsync("MessagesRead", conversationId, UserId, cancellationToken);
        return Ok(new { success = true, data = new { count }, message = (string?)null });
    }

    private Guid UserId => _currentUser.UserId ?? throw new UnauthorizedException("Yêu cầu đăng nhập");
}
