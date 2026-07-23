using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MommyAndSunny.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Infrastructure.Persistence.Configurations
{
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.ToTable("Payments");

            builder.Property(p => p.Provider).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(p => p.TransactionId).HasMaxLength(100);
            builder.Property(p => p.Amount).HasColumnType("numeric(12,2)");
            builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

            // jsonb — kiểu dữ liệu native của PostgreSQL, lưu response gốc từ Momo/VNPay để debug
            builder.Property(p => p.RawCallbackData).HasColumnType("jsonb");
        }
    }
}
