using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MommyAndSunny.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Infrastructure.Persistence.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");

            builder.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(255);
            builder.HasIndex(u => u.Email).IsUnique();
            builder.Property(u => u.NormalizedEmail).IsRequired().HasMaxLength(255);
            builder.HasIndex(u => u.NormalizedEmail).IsUnique();
            builder.Property(u => u.ConcurrencyStamp).IsConcurrencyToken();

            builder.Property(u => u.PasswordHash).HasMaxLength(255);
            builder.Property(u => u.FullName).IsRequired().HasMaxLength(150);
            builder.Property(u => u.PhoneNumber).HasMaxLength(20);

            // Lưu enum dưới dạng chuỗi trong DB (dễ đọc khi query trực tiếp để debug)
            builder.Property(u => u.AuthProvider).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(20).IsRequired();

            builder.HasMany(u => u.Addresses)
                .WithOne(a => a.User)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(u => u.Cart)
                .WithOne(c => c.User)
                .HasForeignKey<Cart>(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Restrict — không cho xóa User nếu đã có Order, để giữ lịch sử đơn hàng
            builder.HasMany(u => u.Orders)
                .WithOne(o => o.User)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

}
