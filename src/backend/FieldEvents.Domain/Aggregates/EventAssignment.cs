namespace FieldEvents.Domain.Aggregates;

/// <summary>
/// Records a technician assignment to a FieldEvent.
/// Full assign/unassign behavior is wired via IAssignEventUseCase and ITransferEventUseCase.
/// Only FieldEvent may create instances via the internal factory method.
/// </summary>
public sealed class EventAssignment
{
    public Guid Id { get; private set; }
    public Guid EventId { get; private set; }
    public Guid TechnicianId { get; private set; }
    public Guid AssignedByUserId { get; private set; }
    public DateTimeOffset AssignedAt { get; private set; }
    public DateTimeOffset? UnassignedAt { get; private set; }

    // Parameterless constructor required by EF Core (Infrastructure configures it via HasNoKey or proxy).
    // Kept private so only FieldEvent can instantiate via the internal factory.
    private EventAssignment() { }

    internal static EventAssignment Create(Guid eventId, Guid technicianId, Guid assignedByUserId)
    {
        return new EventAssignment
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            TechnicianId = technicianId,
            AssignedByUserId = assignedByUserId,
            AssignedAt = DateTimeOffset.UtcNow,
        };
    }

    internal void Unassign()
    {
        UnassignedAt = DateTimeOffset.UtcNow;
    }
}
