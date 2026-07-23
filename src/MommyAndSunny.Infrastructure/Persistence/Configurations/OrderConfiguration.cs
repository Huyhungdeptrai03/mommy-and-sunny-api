using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MommyAndSunny.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Infrastructure.Persistence.Configurations
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.ToTable("Orders");

            builder.Property(o => o.OrderCode).IsRequired().HasMaxLength(30);
            builder.HasIndex(o => o.OrderCode).IsUnique();

            // Cột hay dùng để lọc ở trang Admin quản lý đơn hàng
            builder.HasIndex(o => o.UserId);
            builder.HasIndex(o => o.Status);

            builder.Property(o => o.RecipientName).IsRequired().HasMaxLength(150);
            builder.Property(o => o.RecipientPhone).IsRequired().HasMaxLength(20);
            builder.Property(o => o.ShippingAddress).IsRequired().HasMaxLength(500);

            builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(o => o.PaymentMethod).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(o => o.PaymentStatus).HasConversion<string>().HasMaxLength(20).IsRequired();

            builder.Property(o => o.SubTotal).HasColumnType("numeric(12,2)");
            builder.Property(o => o.ShippingFee).HasColumnType("numeric(12,2)");
            builder.Property(o => o.DiscountAmount).HasColumnType("numeric(12,2)");
            builder.Property(o => o.TotalAmount).HasColumnType("numeric(12,2)");

            builder.Property(o => o.CustomerNote).HasMaxLength(1000);

            builder.HasMany(o => o.Items)
                .WithOne(i => i.Order)
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(o => o.StatusHistory)
                .WithOne(h => h.Order)
                .HasForeignKey(h => h.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(o => o.Payments)
                .WithOne(p => p.Order)
                .HasForeignKey(p => p.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
