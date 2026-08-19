using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MommyAndSunny.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Infrastructure.Persistence.Configurations
{
    public class CouponConfiguration : IEntityTypeConfiguration<Coupon>
    {
        public void Configure(EntityTypeBuilder<Coupon> builder)
        {
            builder.ToTable("Coupons");

            builder.Property(c => c.Code).IsRequired().HasMaxLength(50);
            // Không cho trùng mã — chuẩn hóa CHỮ HOA đã xử lý ở tầng Service trước khi lưu
            builder.HasIndex(c => c.Code).IsUnique();

            builder.Property(c => c.DiscountType).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(c => c.DiscountValue).HasColumnType("numeric(12,2)");
            builder.Property(c => c.MaxDiscountAmount).HasColumnType("numeric(12,2)");
            builder.Property(c => c.MinOrderValue).HasColumnType("numeric(12,2)");

            // Xóa danh mục KHÔNG được kéo theo xóa mã giảm giá — chỉ gỡ liên kết,
            // mã trở về áp dụng cho "toàn đơn hàng" thay vì bị xóa oan
            builder.HasOne(c => c.Category)
                .WithMany()
                .HasForeignKey(c => c.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
