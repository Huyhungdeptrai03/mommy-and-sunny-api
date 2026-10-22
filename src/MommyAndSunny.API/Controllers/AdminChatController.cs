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
[Authorize(Roles = "Admin")]
[EnableRateLimiting("chat")]
[Route("api/admin/chat/conversations")]
public sealed class AdminChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ICurrentUserService _currentUser;
    private readonly IHubContext<ChatHub> _hub;

    public AdminChatController(IChatService chatService, ICurrentUserService currentUser, IHubContext<ChatHub> hub)
    {
        _chatService = chatService;
        _currentUser = currentUser;
        _hub = hub;
    }

    [HttpGet]
    public async Task<IActionResult> GetConversations(
        [FromQuery] ConversationFilterRequest filter, CancellationToken cancellationToken)
    {
        var result = await _chatService.GetAdminConversationsAsync(UserId, filter, cancellationToken);
        return Ok(new { success = true, data = result, message = (string?)null });
    }

    [HttpPost("{conversationId:guid}/assign")]
    public async Task<IActionResult> Assign(Guid conversationId, CancellationToken cancellationToken)
    {
        var result = await _chatService.AssignToMeAsync(conversationId, UserId, cancellationToken);
        await _hub.Clients.Group(ChatHub.GroupName(conversationId))
            .SendAsync("ConversationUpdated", result, cancellationToken);
        return Ok(new { success = true, data = result, message = "Đã nhận cuộc trò chuyện" });
    }

    [HttpPost("{conversationId:guid}/close")]
    public async Task<IActionResult> Close(Guid conversationId, CancellationToken cancellationToken)
    {
        await _chatService.CloseAsync(conversationId, UserId, cancellationToken);
        await _hub.Clients.Group(ChatHub.GroupName(conversationId))
            .SendAsync("ConversationClosed", conversationId, cancellationToken);
        return Ok(new { success = true, data = (object?)null, message = "Đã đóng cuộc trò chuyện" });
    }

    private Guid UserId => _currentUser.UserId ?? throw new UnauthorizedException("Yêu cầu đăng nhập");
}
