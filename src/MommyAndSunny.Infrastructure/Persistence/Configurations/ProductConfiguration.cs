using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MommyAndSunny.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Infrastructure.Persistence.Configurations
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.ToTable("Products");

            builder.Property(p => p.Name).IsRequired().HasMaxLength(255);
            builder.Property(p => p.Slug).IsRequired().HasMaxLength(255);
            builder.HasIndex(p => p.Slug).IsUnique();

            // Cột hay dùng để lọc/tìm kiếm — cần index để trang danh sách sản phẩm load nhanh
            builder.HasIndex(p => p.CategoryId);
            builder.HasIndex(p => p.Status);

            builder.Property(p => p.Brand).HasMaxLength(100);
            builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

            builder.HasMany(p => p.Images)
                .WithOne(i => i.Product)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(p => p.Variants)
                .WithOne(v => v.Product)
                .HasForeignKey(v => v.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Xóa 1 đợt hàng KHÔNG được kéo theo xóa sản phẩm — chỉ gỡ liên kết
            // (sản phẩm trở về trạng thái "bán ngay" thay vì bị xóa oan)
            builder.HasOne(p => p.PurchaseBatch)
                .WithMany(b => b.Products)
                .HasForeignKey(p => p.PurchaseBatchId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }

}
