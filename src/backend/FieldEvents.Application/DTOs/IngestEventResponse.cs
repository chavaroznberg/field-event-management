namespace FieldEvents.Application.DTOs;

public sealed record IngestEventResponse(
    Guid Id,
    string ExternalEventId,
    string SourceId,
    string Title,
    string? Description,
    string Location,
    string Priority,
    string Status,
    DateTimeOffset OccurredAt,
    DateTimeOffset CreatedAt
);
