using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MommyAndSunny.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Infrastructure.Persistence.Configurations
{

    public class PurchaseBatchConfiguration : IEntityTypeConfiguration<PurchaseBatch>
    {
        public void Configure(EntityTypeBuilder<PurchaseBatch> builder)
        {
            builder.ToTable("PurchaseBatches");

            builder.Property(b => b.Name).IsRequired().HasMaxLength(200);
            builder.Property(b => b.CountryOfOrigin).HasMaxLength(100);
        }
    }
}
