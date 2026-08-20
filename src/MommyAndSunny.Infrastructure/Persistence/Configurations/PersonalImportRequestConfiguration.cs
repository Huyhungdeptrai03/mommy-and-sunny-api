using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MommyAndSunny.Domain.Entities;

namespace MommyAndSunny.Infrastructure.Persistence.Configurations
{
    public class PersonalImportRequestConfiguration : IEntityTypeConfiguration<PersonalImportRequest>
    {
        public void Configure(EntityTypeBuilder<PersonalImportRequest> builder)
        {
            builder.ToTable("PersonalImportRequests");

            builder.Property(r => r.ProductName).IsRequired().HasMaxLength(255);
            builder.Property(r => r.ProductLink).HasMaxLength(1000);
            builder.Property(r => r.Notes).HasMaxLength(2000);
            builder.Property(r => r.ImageUrl).HasMaxLength(500);
            builder.Property(r => r.AdminNote).HasMaxLength(2000);

            builder.Property(r => r.CustomerName).IsRequired().HasMaxLength(150);
            builder.Property(r => r.CustomerPhone).IsRequired().HasMaxLength(20);
            builder.Property(r => r.CustomerEmail).HasMaxLength(255);

            builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.HasIndex(r => r.Status);

            // SetNull thay vì Cascade — xoá tài khoản khách không nên xoá theo lịch sử
            // yêu cầu xách tay mà admin có thể vẫn cần tra cứu
            builder.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
