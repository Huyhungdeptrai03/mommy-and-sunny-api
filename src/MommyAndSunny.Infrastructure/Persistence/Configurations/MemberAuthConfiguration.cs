using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MommyAndSunny.Domain.Entities;
namespace MommyAndSunny.Infrastructure.Persistence.Configurations;

public class AuthSessionConfiguration : IEntityTypeConfiguration<AuthSession>
{
    public void Configure(EntityTypeBuilder<AuthSession> b)
    {
        b.ToTable("AuthSessions");
        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasIndex(x => x.UserId);
    }
}
public class ExternalLoginConfiguration : IEntityTypeConfiguration<ExternalLogin>
{
    public void Configure(EntityTypeBuilder<ExternalLogin> b)
    {
        b.ToTable("ExternalLogins");
        b.Property(x => x.Provider).HasMaxLength(20);
        b.Property(x => x.ProviderSubject).HasMaxLength(255).UseCollation("C");
        b.HasIndex(x => new { x.Provider, x.ProviderSubject }).IsUnique();
        b.HasIndex(x => new { x.UserId, x.Provider }).IsUnique();
        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
public class ExternalHandoffConfiguration : IEntityTypeConfiguration<ExternalHandoff>
{
    public void Configure(EntityTypeBuilder<ExternalHandoff> b)
    {
        b.ToTable("ExternalHandoffs");
        b.Property(x => x.CodeHash).HasMaxLength(64);
        b.Property(x => x.BrowserHash).HasMaxLength(64);
        b.Property(x => x.Provider).HasMaxLength(20);
        b.Property(x => x.ProviderSubject).HasMaxLength(255).UseCollation("C");
        b.Property(x => x.ProviderEmail).HasMaxLength(255);
        b.Property(x => x.PendingEmail).HasMaxLength(255);
        b.Property(x => x.FullName).HasMaxLength(150);
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasIndex(x => x.CodeHash).IsUnique();
        b.HasIndex(x => x.ExpiresAt);
    }
}
