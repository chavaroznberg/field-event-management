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
        services.AddDbContext<FieldEventsDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sql => sql.MigrationsAssembly(typeof(FieldEventsDbContext).Assembly.FullName)));

        services.AddScoped<IIngestEventUseCase, IngestEventUseCase>();
        services.AddScoped<IGetRecentEventsUseCase, GetRecentEventsUseCase>();

        return services;
    }
}
