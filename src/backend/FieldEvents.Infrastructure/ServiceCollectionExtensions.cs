using FieldEvents.Application.Interfaces;
using FieldEvents.Infrastructure.Persistence;
using FieldEvents.Infrastructure.UseCases;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FieldEvents.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        // Local-dev shortcut: a connection string starting with "Data Source=" is SQLite.
        // Production always uses SQL Server (the default branch).
        var isSqlite = connectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase);

        services.AddDbContext<FieldEventsDbContext>(options =>
        {
            if (isSqlite)
                options.UseSqlite(connectionString);
            else
                options.UseSqlServer(
                    connectionString,
                    sql => sql.MigrationsAssembly(typeof(FieldEventsDbContext).Assembly.FullName));
        });

        services.AddScoped<IIngestEventUseCase, IngestEventUseCase>();
        services.AddScoped<IGetRecentEventsUseCase, GetRecentEventsUseCase>();

        return services;
    }
}
