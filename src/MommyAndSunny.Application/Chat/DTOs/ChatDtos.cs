using MommyAndSunny.Domain.Enums;

namespace MommyAndSunny.Application.Chat.DTOs;

public sealed class SendChatMessageRequest
{
    public string? Content { get; set; }
    public string? ImageUrl { get; set; }
}

public sealed class ConversationFilterRequest
{
    public ConversationStatus? Status { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class ChatMessageFilterRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public sealed class ConversationDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public Guid? AssignedAdminId { get; set; }
    public string? AssignedAdminName { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime LastMessageAt { get; set; }
    public int UnreadCount { get; set; }
}

public sealed class ChatMessageDto
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string SenderRole { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? ImageUrl { get; set; }
    public IReadOnlyList<ChatProductTagDto> ProductTags { get; set; } = [];
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class ChatProductTagDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int Start { get; set; }
    public int Length { get; set; }
}
