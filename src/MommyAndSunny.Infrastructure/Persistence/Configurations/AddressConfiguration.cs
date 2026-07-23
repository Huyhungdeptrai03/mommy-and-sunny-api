using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MommyAndSunny.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Infrastructure.Persistence.Configurations
{
    public class AddressConfiguration : IEntityTypeConfiguration<Address>
    {
        public void Configure(EntityTypeBuilder<Address> builder)
        {
            builder.ToTable("Addresses");

            builder.Property(a => a.RecipientName).IsRequired().HasMaxLength(150);
            builder.Property(a => a.PhoneNumber).IsRequired().HasMaxLength(20);
            builder.Property(a => a.Province).IsRequired().HasMaxLength(100);
            builder.Property(a => a.District).IsRequired().HasMaxLength(100);
            builder.Property(a => a.Ward).IsRequired().HasMaxLength(100);
            builder.Property(a => a.DetailAddress).IsRequired().HasMaxLength(255);

            // Tăng tốc truy vấn "lấy danh sách địa chỉ của 1 user"
            builder.HasIndex(a => a.UserId);
        }
    }
}
