using FieldEvents.Domain.Enums;

namespace FieldEvents.Application.Commands;

public sealed record IngestEventCommand(
    string ExternalEventId,
    string SourceId,
    string Title,
    string? Description,
    string Location,
    EventPriority Priority,
    DateTimeOffset OccurredAt
);
