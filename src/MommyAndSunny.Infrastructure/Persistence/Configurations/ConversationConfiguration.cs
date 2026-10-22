using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MommyAndSunny.Domain.Entities;

namespace MommyAndSunny.Infrastructure.Persistence.Configurations;

public sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("Conversations");
        builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.HasIndex(item => item.LastMessageAt);
        builder.HasIndex(item => new { item.CustomerId, item.Status });
        builder.HasIndex(item => item.CustomerId).IsUnique()
            .HasFilter("\"Status\" <> 'Closed'");
        builder.HasOne(item => item.Customer).WithMany().HasForeignKey(item => item.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.AssignedAdmin).WithMany().HasForeignKey(item => item.AssignedAdminId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
