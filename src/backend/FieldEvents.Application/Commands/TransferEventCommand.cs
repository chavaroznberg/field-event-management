namespace FieldEvents.Application.Commands;

public sealed record TransferEventCommand(
    Guid EventId,
    Guid FromTechnicianId,
    Guid ToTechnicianId,
    string? Reason);
