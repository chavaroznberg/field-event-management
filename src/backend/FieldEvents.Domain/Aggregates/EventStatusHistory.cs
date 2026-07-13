using FieldEvents.Domain.Enums;

namespace FieldEvents.Domain.Aggregates;

/// <summary>
/// An immutable record of a single status change on a FieldEvent.
/// Only FieldEvent may create instances — enforced by the internal constructor.
///
/// The private parameterless constructor and private setters exist solely to allow
/// EF Core (in the Infrastructure project) to materialise rows from the database
/// via reflection. No EF Core package is referenced here.
/// </summary>
public sealed class EventStatusHistory
{
    // EF Core requires either a parameterless constructor or constructor injection
    // where all mapped properties appear as parameters. Because Id is generated
    // inside the internal constructor (not a parameter), we provide a parameterless
    // one so EF Core can instantiate the entity and then set properties individually.
    private EventStatusHistory() { }

    public Guid Id { get; private set; }
    public Guid EventId { get; private set; }

    /// <summary>Null only for the initial history entry created when the event is first persisted.</summary>
    public EventStatus? PreviousStatus { get; private set; }

    public EventStatus NewStatus { get; private set; }
    public Guid ChangedByUserId { get; private set; }
    public DateTimeOffset ChangedAt { get; private set; }
    public string? Comment { get; private set; }

    internal EventStatusHistory(
        Guid eventId,
        EventStatus? previousStatus,
        EventStatus newStatus,
        Guid changedByUserId,
        DateTimeOffset changedAt,
        string? comment)
    {
        Id = Guid.NewGuid();
        EventId = eventId;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        ChangedByUserId = changedByUserId;
        ChangedAt = changedAt;
        Comment = comment;
    }
}
