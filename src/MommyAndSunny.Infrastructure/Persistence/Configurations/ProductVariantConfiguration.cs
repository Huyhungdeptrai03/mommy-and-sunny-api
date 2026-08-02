using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MommyAndSunny.Domain.Entities;

namespace MommyAndSunny.Infrastructure.Persistence.Configurations;

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
        builder.Property(v => v.CompareAtPrice).HasColumnType("numeric(12,2)");

        // AvailableQuantity là property tính runtime (StockQuantity - ReservedQuantity),
        // KHÔNG map vào cột database — bắt buộc Ignore(), nếu không EF Core sẽ báo lỗi khi migrate
        builder.Ignore(v => v.AvailableQuantity);

        // Dùng cột hệ thống "xmin" có sẵn trên mọi bảng PostgreSQL làm concurrency token —
        // Postgres TỰ ĐỘNG thay đổi giá trị này mỗi khi row được UPDATE (cơ chế MVCC gốc).
        // Dùng cú pháp EF Core gốc (shadow property + IsRowVersion) thay vì extension method
        // riêng của Npgsql — ổn định hơn, không phụ thuộc đúng version cụ thể giữa các package
        builder.Property<uint>("xmin")
            .IsRowVersion()
            .HasColumnName("xmin");
    }
}