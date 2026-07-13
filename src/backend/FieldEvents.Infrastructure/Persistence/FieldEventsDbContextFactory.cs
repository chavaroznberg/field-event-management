using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FieldEvents.Infrastructure.Persistence;

/// <summary>
/// Used exclusively by the EF Core design-time tooling (dotnet ef migrations add …).
/// The connection string here is for local development only — it is never used at
/// runtime and contains no real credentials.
/// </summary>
internal sealed class FieldEventsDbContextFactory : IDesignTimeDbContextFactory<FieldEventsDbContext>
{
    public FieldEventsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<FieldEventsDbContext>()
            .UseSqlServer(
                "Server=localhost,1433;Database=FieldEventsDb;User Id=sa;Password=PLACEHOLDER;TrustServerCertificate=True",
                sql => sql.MigrationsAssembly(typeof(FieldEventsDbContext).Assembly.FullName))
            .Options;

        return new FieldEventsDbContext(options);
    }
}
