using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MommyAndSunny.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Infrastructure.Persistence.Configurations
{
    public class OrderStatusHistoryConfiguration : IEntityTypeConfiguration<OrderStatusHistory>
    {
        public void Configure(EntityTypeBuilder<OrderStatusHistory> builder)
        {
            builder.ToTable("OrderStatusHistory");

            builder.Property(h => h.FromStatus).HasConversion<string>().HasMaxLength(20);
            builder.Property(h => h.ToStatus).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(h => h.Note).HasMaxLength(500);

            // SetNull — nếu tài khoản admin bị xóa sau này, lịch sử vẫn giữ nguyên,
            // chỉ mất thông tin "ai đã đổi", không mất bản ghi lịch sử
            builder.HasOne(h => h.ChangedByUser)
                .WithMany()
                .HasForeignKey(h => h.ChangedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }

}
