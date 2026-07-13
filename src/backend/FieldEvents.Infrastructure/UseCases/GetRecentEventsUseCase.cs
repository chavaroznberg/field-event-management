using FieldEvents.Application.DTOs;
using FieldEvents.Application.Interfaces;
using FieldEvents.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FieldEvents.Infrastructure.UseCases;

public sealed class GetRecentEventsUseCase : IGetRecentEventsUseCase
{
    private readonly FieldEventsDbContext _db;

    public GetRecentEventsUseCase(FieldEventsDbContext db) => _db = db;

    public async Task<IReadOnlyList<IngestEventResponse>> ExecuteAsync(CancellationToken ct = default)
    {
        // SQLite (local-dev fallback) cannot translate DateTimeOffset ordering in SQL.
        // For SQL Server, push OrderBy into SQL so Take(100) returns the 100 most recent.
        // Client-side sort ensures correct ordering in both cases.
        IQueryable<Domain.Aggregates.FieldEvent> query = _db.FieldEvents.AsNoTracking();
        if (_db.Database.IsSqlServer())
            query = query.OrderByDescending(e => e.CreatedAt);

        var entities = await query.Take(100).ToListAsync(ct);

        return entities
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new IngestEventResponse(
                e.Id,
                e.ExternalEventId,
                e.SourceId,
                e.Title,
                e.Description,
                e.Location,
                e.Priority.ToString(),
                e.Status.ToString(),
                e.OccurredAt,
                e.CreatedAt))
            .ToList();
    }
}
