using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MommyAndSunny.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Infrastructure.Persistence.Configurations
{
    public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
    {
        public void Configure(EntityTypeBuilder<OrderItem> builder)
        {
            builder.ToTable("OrderItems");

            builder.Property(i => i.ProductNameSnapshot).IsRequired().HasMaxLength(255);
            builder.Property(i => i.SkuSnapshot).IsRequired().HasMaxLength(50);
            builder.Property(i => i.VariantDescriptionSnapshot).HasMaxLength(100);
            builder.Property(i => i.PurchaseBatchNameSnapshot).HasMaxLength(200);

            builder.Property(i => i.UnitPrice).HasColumnType("numeric(12,2)");
            builder.Property(i => i.Subtotal).HasColumnType("numeric(12,2)");

            // Restrict — không cho xóa ProductVariant nếu đã từng xuất hiện trong 1 đơn hàng,
            // vì OrderItem vẫn giữ FK tham chiếu (dù hiển thị dùng bản snapshot)
            builder.HasOne(i => i.ProductVariant)
                .WithMany(v => v.OrderItems)
                .HasForeignKey(i => i.ProductVariantId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

}
