namespace FieldEvents.Application.Commands;

public sealed record AssignEventCommand(Guid EventId, Guid TechnicianId, Guid AssignedByUserId);
