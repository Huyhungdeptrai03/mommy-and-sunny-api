using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MommyAndSunny.Domain.Entities;

namespace MommyAndSunny.Infrastructure.Persistence.Configurations;

public sealed class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("ChatMessages");
        builder.Property(item => item.Content).HasMaxLength(2_000);
        builder.Property(item => item.ImageUrl).HasMaxLength(1_000);
        builder.HasIndex(item => new { item.ConversationId, item.CreatedAt });
        builder.HasIndex(item => new { item.ConversationId, item.IsRead });
        builder.HasOne(item => item.Conversation).WithMany(item => item.Messages)
            .HasForeignKey(item => item.ConversationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(item => item.Sender).WithMany().HasForeignKey(item => item.SenderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
