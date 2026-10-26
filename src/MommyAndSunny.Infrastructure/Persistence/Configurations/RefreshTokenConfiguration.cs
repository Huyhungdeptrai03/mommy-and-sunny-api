using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MommyAndSunny.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Infrastructure.Persistence.Configurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("RefreshTokens");

            builder.Property(r => r.TokenHash).IsRequired().HasMaxLength(255);
            builder.HasIndex(r => r.TokenHash).IsUnique();
            builder.HasIndex(r => r.UserId);
            builder.Property(r => r.ConsumedAt).IsConcurrencyToken();
            builder.Property(r => r.RevokedAt).IsConcurrencyToken();
            builder.HasOne(r => r.Session).WithMany().HasForeignKey(r => r.SessionId).OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Ignore(r => r.IsActive);
        }
    }
}
