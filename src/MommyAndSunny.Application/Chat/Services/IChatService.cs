using MommyAndSunny.Application.Chat.DTOs;
using MommyAndSunny.Application.Common.Models;

namespace MommyAndSunny.Application.Chat.Services;

public interface IChatService
{
    Task<ConversationDto> GetOrCreateForCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<ConversationDto> GetConversationAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default);
    Task<PagedResult<ConversationDto>> GetAdminConversationsAsync(Guid adminId, ConversationFilterRequest filter, CancellationToken cancellationToken = default);
    Task<PagedResult<ChatMessageDto>> GetMessagesAsync(Guid conversationId, Guid userId, ChatMessageFilterRequest filter, CancellationToken cancellationToken = default);
    Task<ChatMessageDto> SendMessageAsync(Guid conversationId, Guid senderId, SendChatMessageRequest request, CancellationToken cancellationToken = default);
    Task<ConversationDto> AssignToMeAsync(Guid conversationId, Guid adminId, CancellationToken cancellationToken = default);
    Task CloseAsync(Guid conversationId, Guid adminId, CancellationToken cancellationToken = default);
    Task<int> MarkAsReadAsync(Guid conversationId, Guid readerId, CancellationToken cancellationToken = default);
    Task<bool> CanAccessAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default);
}
