using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MommyAndSunny.Domain.Entities;

namespace MommyAndSunny.Infrastructure.Persistence.Configurations
{
    public class PolicyConfiguration : IEntityTypeConfiguration<Policy>
    {
        // Ngày cố định cho dữ liệu seed — HasData được biên dịch tĩnh vào migration nên
        // không dùng DateTime.UtcNow (giá trị sẽ đổi mỗi lần build lại migration)
        private static readonly DateTime SeedDate = new(2026, 8, 20, 0, 0, 0, DateTimeKind.Utc);

        public void Configure(EntityTypeBuilder<Policy> builder)
        {
            builder.ToTable("Policies");

            builder.Property(p => p.Slug).IsRequired().HasMaxLength(50);
            builder.HasIndex(p => p.Slug).IsUnique();

            builder.Property(p => p.Title).IsRequired().HasMaxLength(255);
            builder.Property(p => p.Content).IsRequired();

            builder.HasData(
                new Policy
                {
                    Id = Guid.Parse("a1a1a1a1-0001-0001-0001-000000000001"),
                    Slug = "doi-tra",
                    Title = "Chính sách đổi trả",
                    Content = "Nội dung chính sách đổi trả đang được cập nhật.",
                    CreatedAt = SeedDate,
                    UpdatedAt = SeedDate
                },
                new Policy
                {
                    Id = Guid.Parse("a1a1a1a1-0002-0002-0002-000000000002"),
                    Slug = "van-chuyen",
                    Title = "Chính sách vận chuyển",
                    Content = "Nội dung chính sách vận chuyển đang được cập nhật.",
                    CreatedAt = SeedDate,
                    UpdatedAt = SeedDate
                },
                new Policy
                {
                    Id = Guid.Parse("a1a1a1a1-0003-0003-0003-000000000003"),
                    Slug = "bao-mat",
                    Title = "Chính sách bảo mật",
                    Content = "Nội dung chính sách bảo mật đang được cập nhật.",
                    CreatedAt = SeedDate,
                    UpdatedAt = SeedDate
                },
                new Policy
                {
                    Id = Guid.Parse("a1a1a1a1-0004-0004-0004-000000000004"),
                    Slug = "dieu-khoan",
                    Title = "Điều khoản dịch vụ",
                    Content = "Nội dung điều khoản dịch vụ đang được cập nhật.",
                    CreatedAt = SeedDate,
                    UpdatedAt = SeedDate
                }
            );
        }
    }
}
