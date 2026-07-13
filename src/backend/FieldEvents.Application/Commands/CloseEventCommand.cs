namespace FieldEvents.Application.Commands;

public sealed record CloseEventCommand(
    Guid EventId,
    Guid ClosedByUserId,
    string? Resolution);
