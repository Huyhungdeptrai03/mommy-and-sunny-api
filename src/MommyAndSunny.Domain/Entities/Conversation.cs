using MommyAndSunny.Domain.Common;
using MommyAndSunny.Domain.Enums;

namespace MommyAndSunny.Domain.Entities;

public class Conversation : BaseEntity
{
    public Guid CustomerId { get; set; }
    public User Customer { get; set; } = null!;
    public Guid? AssignedAdminId { get; set; }
    public User? AssignedAdmin { get; set; }
    public ConversationStatus Status { get; set; } = ConversationStatus.Waiting;
    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
