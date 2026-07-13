using FieldEvents.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FieldEvents.IntegrationTests;

/// <summary>
/// Starts the full ASP.NET Core pipeline with an in-process SQLite database instead of
/// SQL Server. SQLite enforces UNIQUE constraints, which lets integration tests verify
/// the idempotency guarantee without requiring a running SQL Server instance.
///
/// Limitation: rowversion (optimistic concurrency) is not modelled in SQLite.
/// That feature is covered by the SQL Server configuration but cannot be exercised here.
/// </summary>
public sealed class FieldEventsWebApplicationFactory
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    // A single shared connection keeps the in-memory database alive for the test run.
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Provide a dummy SQL Server connection string so Program.cs does not throw
        // before we replace the DbContext registration below.
        builder.ConfigureAppConfiguration((_, cfg) =>
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Server=unused-in-tests;Database=FieldEventsTest;Trusted_Connection=True;"
            }));

        builder.ConfigureServices(services =>
        {
            // EF Core 10 registers each provider's IDatabaseProvider as a singleton in the
            // application's DI container (via XxxOptionsExtension.ApplyServices). When both
            // SqlServer (from AddInfrastructure) and Sqlite registrations coexist, EF Core
            // throws "Only a single database provider can be registered."
            //
            // Fix: build an *isolated* EF Core internal service provider that contains only
            // the SQLite provider, and tell each test DbContext to use it exclusively via
            // UseInternalServiceProvider. This completely bypasses the application DI for
            // EF Core's internal wiring.
            var efSqliteOnly = new ServiceCollection()
                .AddEntityFrameworkSqlite()
                .BuildServiceProvider();

            // Remove the SQL Server DbContext registered by AddInfrastructure.
            var toRemove = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<FieldEventsDbContext>)
                         || d.ServiceType == typeof(DbContextOptions)
                         || d.ServiceType == typeof(FieldEventsDbContext))
                .ToList();
            foreach (var d in toRemove) services.Remove(d);

            services.AddScoped(_ =>
            {
                var opts = new DbContextOptionsBuilder<FieldEventsDbContext>()
                    .UseSqlite(_connection)
                    .UseInternalServiceProvider(efSqliteOnly)
                    .Options;
                return new FieldEventsDbContext(opts);
            });
        });
    }

    /// <summary>Opens the connection and applies EnsureCreated to build the schema.</summary>
    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FieldEventsDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    /// <summary>Allows tests to assert directly against the database.</summary>
    public async Task<int> CountEventsAsync(string sourceId, string externalEventId)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FieldEventsDbContext>();
        return await db.FieldEvents
            .CountAsync(e => e.SourceId == sourceId && e.ExternalEventId == externalEventId);
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
