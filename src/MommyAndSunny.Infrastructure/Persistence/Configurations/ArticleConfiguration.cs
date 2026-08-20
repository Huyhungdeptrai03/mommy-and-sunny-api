using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MommyAndSunny.Domain.Entities;

namespace MommyAndSunny.Infrastructure.Persistence.Configurations
{
    public class ArticleConfiguration : IEntityTypeConfiguration<Article>
    {
        public void Configure(EntityTypeBuilder<Article> builder)
        {
            builder.ToTable("Articles");

            builder.Property(a => a.Title).IsRequired().HasMaxLength(255);
            builder.Property(a => a.Slug).IsRequired().HasMaxLength(255);
            builder.HasIndex(a => a.Slug).IsUnique();

            builder.Property(a => a.Excerpt).HasMaxLength(500);
            builder.Property(a => a.Content).IsRequired();
            builder.Property(a => a.CoverImageUrl).HasMaxLength(500);
            builder.Property(a => a.AuthorName).HasMaxLength(150);

            // Lưu enum dưới dạng chuỗi (giống Product.Status) — dễ đọc trực tiếp trong DB,
            // và không bị lệch số nếu sau này thêm/đổi thứ tự giá trị enum
            builder.Property(a => a.Category).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.HasIndex(a => a.Category);
            builder.HasIndex(a => a.Status);
        }
    }
}
