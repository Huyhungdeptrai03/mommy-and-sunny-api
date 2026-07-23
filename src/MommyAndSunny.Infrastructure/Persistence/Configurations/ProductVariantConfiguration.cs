using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MommyAndSunny.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Infrastructure.Persistence.Configurations
{
    public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
    {
        public void Configure(EntityTypeBuilder<ProductVariant> builder)
        {
            builder.ToTable("ProductVariants", t =>
            {
                // Check constraint ở tầng database — lớp bảo vệ cuối cùng chống tồn kho âm,
                // phòng trường hợp có bug ở tầng Application vẫn không thể ghi dữ liệu sai vào DB
                t.HasCheckConstraint("CK_ProductVariants_StockQuantity", "\"StockQuantity\" >= 0");
                t.HasCheckConstraint("CK_ProductVariants_ReservedQuantity", "\"ReservedQuantity\" >= 0");
            });

            builder.Property(v => v.Sku).IsRequired().HasMaxLength(50);
            builder.HasIndex(v => v.Sku).IsUnique();

            builder.Property(v => v.Size).HasMaxLength(20);
            builder.Property(v => v.Color).HasMaxLength(50);
            builder.Property(v => v.Price).HasColumnType("numeric(12,2)");

            // AvailableQuantity là property tính runtime (StockQuantity - ReservedQuantity),
            // KHÔNG map vào cột database — bắt buộc Ignore(), nếu không EF Core sẽ báo lỗi khi migrate
            builder.Ignore(v => v.AvailableQuantity);

            // Shadow property RowVersion — dùng optimistic concurrency để tránh 2 request
            // cùng lúc ghi đè sai lệch tồn kho (race condition đã bàn ở tài liệu nghiệp vụ).
            // Đây là shadow property (không cần khai báo trong Entity) vì nó thuộc về mối quan tâm
            // của tầng Infrastructure, không phải business logic của Domain.
            builder.Property<uint>("RowVersion").IsRowVersion();
        }
    }

}
