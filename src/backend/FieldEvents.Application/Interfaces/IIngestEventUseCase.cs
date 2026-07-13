using FieldEvents.Application.Commands;
using FieldEvents.Application.DTOs;

namespace FieldEvents.Application.Interfaces;

public interface IIngestEventUseCase
{
    /// <summary>
    /// Returns the event (new or pre-existing) and whether it was just created.
    /// WasCreated=false means an identical SourceId+ExternalEventId already existed —
    /// the caller should return 200 OK instead of 201 Created.
    /// </summary>
    Task<(IngestEventResponse Event, bool WasCreated)> ExecuteAsync(
        IngestEventCommand command, CancellationToken ct = default);
}
