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
        var entities = await _db.FieldEvents
            .AsNoTracking()
            .OrderByDescending(e => e.CreatedAt)
            .Take(100)
            .ToListAsync(ct);

        return entities
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
