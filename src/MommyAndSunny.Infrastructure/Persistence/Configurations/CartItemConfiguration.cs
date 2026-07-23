using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MommyAndSunny.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Infrastructure.Persistence.Configurations
{
    public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
    {
        public void Configure(EntityTypeBuilder<CartItem> builder)
        {
            builder.ToTable("CartItems");

            // Ràng buộc nghiệp vụ: mỗi biến thể chỉ xuất hiện 1 dòng trong giỏ —
            // khi thêm lại biến thể đã có, Application phải update Quantity thay vì tạo dòng mới
            builder.HasIndex(i => new { i.CartId, i.ProductVariantId }).IsUnique();

            // Restrict — không cho xóa ProductVariant nếu đang nằm trong giỏ hàng của ai đó
            builder.HasOne(i => i.ProductVariant)
                .WithMany(v => v.CartItems)
                .HasForeignKey(i => i.ProductVariantId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

}
