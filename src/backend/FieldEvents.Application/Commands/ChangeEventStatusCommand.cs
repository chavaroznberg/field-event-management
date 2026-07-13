using FieldEvents.Domain.Enums;

namespace FieldEvents.Application.Commands;

public sealed record ChangeEventStatusCommand(
    Guid EventId,
    EventStatus NewStatus,
    Guid ChangedByUserId);
