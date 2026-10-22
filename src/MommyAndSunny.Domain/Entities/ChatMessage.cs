using MommyAndSunny.Domain.Common;

namespace MommyAndSunny.Domain.Entities;

public class ChatMessage : BaseEntity
{
    public Guid ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;
    public Guid SenderId { get; set; }
    public User Sender { get; set; } = null!;
    public string? Content { get; set; }
    public string? ImageUrl { get; set; }
    public string? ProductTagsJson { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
}
