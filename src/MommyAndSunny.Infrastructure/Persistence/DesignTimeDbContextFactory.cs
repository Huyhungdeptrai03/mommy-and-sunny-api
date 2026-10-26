using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace MommyAndSunny.Infrastructure.Persistence;

// EF scaffolding must never build the web host or read deployment connection strings.
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseNpgsql("Host=127.0.0.1;Port=1;Database=design_time_only;Username=unused;Password=unused")
        .Options);
}
