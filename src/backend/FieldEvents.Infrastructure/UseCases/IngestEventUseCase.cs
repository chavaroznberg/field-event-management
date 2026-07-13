using FieldEvents.Application.Commands;
using FieldEvents.Application.DTOs;
using FieldEvents.Application.Interfaces;
using FieldEvents.Domain.Aggregates;
using FieldEvents.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FieldEvents.Infrastructure.UseCases;

public sealed class IngestEventUseCase : IIngestEventUseCase
{
    // Fixed service account ID for the Agent ingestion path.
    // Replace with real identity once JWT authentication is implemented.
    private static readonly Guid AgentServiceAccountId =
        Guid.Parse("00000000-0000-0000-0000-000000000001");

    private readonly FieldEventsDbContext _db;

    public IngestEventUseCase(FieldEventsDbContext db) => _db = db;

    public async Task<(IngestEventResponse Event, bool WasCreated)> ExecuteAsync(
        IngestEventCommand command, CancellationToken ct = default)
    {
        // Optimistic pre-check: avoid creating the aggregate if we already have this event.
        var existing = await _db.FieldEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.SourceId == command.SourceId && e.ExternalEventId == command.ExternalEventId,
                ct);

        if (existing is not null)
            return (MapToResponse(existing), WasCreated: false);

        var fieldEvent = FieldEvent.Create(
            command.ExternalEventId,
            command.SourceId,
            command.Title,
            command.Description,
            command.Location,
            command.Priority,
            command.OccurredAt,
            AgentServiceAccountId);

        _db.FieldEvents.Add(fieldEvent);

        try
        {
            await _db.SaveChangesAsync(ct);
            return (MapToResponse(fieldEvent), WasCreated: true);
        }
        catch (DbUpdateException)
        {
            // A concurrent request beat us between the pre-check and the insert.
            // Re-query: if we find the row it was a unique-constraint race; otherwise re-throw.
            var raceExisting = await _db.FieldEvents
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    e => e.SourceId == command.SourceId && e.ExternalEventId == command.ExternalEventId,
                    ct);

            if (raceExisting is null) throw;

            return (MapToResponse(raceExisting), WasCreated: false);
        }
    }

    private static IngestEventResponse MapToResponse(FieldEvent e) => new(
        e.Id,
        e.ExternalEventId,
        e.SourceId,
        e.Title,
        e.Description,
        e.Location,
        e.Priority.ToString(),
        e.Status.ToString(),
        e.OccurredAt,
        e.CreatedAt);
}
