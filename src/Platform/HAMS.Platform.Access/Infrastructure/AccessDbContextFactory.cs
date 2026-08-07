using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace HAMS.Platform.Access.Infrastructure;

/// <summary>
/// Builds its own <see cref="DbContextOptions{TContext}"/> directly rather than resolving one from
/// the DI container - see <see cref="PlatformAccessExtensions.AddPlatformAccess"/>'s registration
/// comment for why (the AddDbContextFactory extension method's own DbContextOptions registration
/// collides with AddDbContext's scoped one for the same context type).
/// </summary>
internal sealed class AccessDbContextFactory(IConfiguration configuration) : IDbContextFactory<AccessDbContext>
{
    public AccessDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AccessDbContext>()
            .UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "access"))
            .Options;
        return new AccessDbContext(options);
    }
}
