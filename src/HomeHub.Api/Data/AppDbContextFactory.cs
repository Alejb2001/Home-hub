using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HomeHub.Api.Data;

/// <summary>
/// Design-time factory used by EF Core CLI tools (dotnet ef migrations add).
/// Not used at runtime.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        // Use a local PostgreSQL for migrations; override with DATABASE_URL at runtime.
        optionsBuilder.UseNpgsql("Host=localhost;Database=homehub;Username=homehub;Password=homehub_local");
        return new AppDbContext(optionsBuilder.Options);
    }
}
