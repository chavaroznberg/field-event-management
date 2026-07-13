using FieldEvents.Domain.Enums;
using FieldEvents.Domain.Exceptions;

namespace FieldEvents.Domain.Aggregates;

/// <summary>
/// Aggregate root for a field event.
/// All state mutations flow through this class to enforce business rules.
/// EF Core maps this entity in Infrastructure using Fluent API — no annotations here.
/// </summary>
public sealed class FieldEvent
{
    // ---------------------------------------------------------------------------
    // State machine — the complete transition table.
    // A missing key means no transitions are allowed (terminal state).
    // Defined once at class load; never modified at runtime.
    // ---------------------------------------------------------------------------
    private static readonly Dictionary<EventStatus, HashSet<EventStatus>> AllowedTransitions = new()
    {
        [EventStatus.New] = [EventStatus.Assigned, EventStatus.Cancelled],
        [EventStatus.Assigned] = [EventStatus.New, EventStatus.InProgress, EventStatus.Cancelled],
        [EventStatus.InProgress] = [EventStatus.WaitingForInformation, EventStatus.Completed, EventStatus.Cancelled],
        [EventStatus.WaitingForInformation] = [EventStatus.InProgress, EventStatus.Cancelled],
        [EventStatus.Completed] = [],   // terminal
        [EventStatus.Cancelled] = [],   // terminal
    };

    // ---------------------------------------------------------------------------
    // Private backing collections — owned by this aggregate root.
    // EF Core accesses them via HasField("_statusHistory") in Infrastructure.
    // ---------------------------------------------------------------------------
    private readonly List<EventStatusHistory> _statusHistory = [];
    private readonly List<EventAssignment> _assignments = [];

    // Parameterless private constructor keeps EF Core and object initializers happy
    // while preventing external instantiation.
    private FieldEvent() { }

    // ---------------------------------------------------------------------------
    // Identity and data properties
    // Private setters prevent external mutation; only this class and EF Core write them.
    // ---------------------------------------------------------------------------
    public Guid Id { get; private set; }
    public string ExternalEventId { get; private set; } = string.Empty;
    public string SourceId { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string Location { get; private set; } = string.Empty;
    public EventPriority Priority { get; private set; }

    /// <summary>
    /// Current status. No public setter — the only legal mutation path is ChangeStatus().
    /// </summary>
    public EventStatus Status { get; private set; }

    public Guid? AssignedTechnicianId { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // EF Core optimistic concurrency token — Infrastructure configures it with IsRowVersion().
    public byte[] RowVersion { get; private set; } = [];

    // ---------------------------------------------------------------------------
    // Read-only views of the owned collections.
    // Callers can read but not mutate the lists.
    // ---------------------------------------------------------------------------
    public IReadOnlyCollection<EventStatusHistory> StatusHistory => _statusHistory.AsReadOnly();
    public IReadOnlyCollection<EventAssignment> Assignments => _assignments.AsReadOnly();

    // ---------------------------------------------------------------------------
    // Factory method — the only way to create a valid FieldEvent.
    // Using a static factory (rather than a public constructor) lets us:
    //   1. Give the method a meaningful name.
    //   2. Add the initial history entry atomically with creation.
    //   3. Enforce all creation invariants in one place.
    // ---------------------------------------------------------------------------
    public static FieldEvent Create(
        string externalEventId,
        string sourceId,
        string title,
        string? description,
        string location,
        EventPriority priority,
        DateTimeOffset occurredAt,
        Guid createdByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalEventId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(location);

        var now = DateTimeOffset.UtcNow;

        var fieldEvent = new FieldEvent
        {
            Id = Guid.NewGuid(),
            ExternalEventId = externalEventId,
            SourceId = sourceId,
            Title = title,
            Description = description,
            Location = location,
            Priority = priority,
            Status = EventStatus.New,
            OccurredAt = occurredAt,
            CreatedAt = now,
            UpdatedAt = now,
        };

        // Record the initial status atomically with creation.
        // PreviousStatus is null — there was no previous state.
        fieldEvent._statusHistory.Add(new EventStatusHistory(
            eventId: fieldEvent.Id,
            previousStatus: null,
            newStatus: EventStatus.New,
            changedByUserId: createdByUserId,
            changedAt: now,
            comment: null));

        return fieldEvent;
    }

    // ---------------------------------------------------------------------------
    // State transition — the only legal way to change Status.
    // Validates the transition BEFORE making any change.
    // If the transition is invalid, the aggregate is left completely unmodified.
    // ---------------------------------------------------------------------------
    public void ChangeStatus(EventStatus newStatus, Guid changedByUserId, string? comment = null)
    {
        EnsureTransitionIsAllowed(Status, newStatus);

        var previousStatus = Status;
        var changedAt = DateTimeOffset.UtcNow;

        Status = newStatus;
        UpdatedAt = changedAt;

        _statusHistory.Add(new EventStatusHistory(
            eventId: Id,
            previousStatus: previousStatus,
            newStatus: newStatus,
            changedByUserId: changedByUserId,
            changedAt: changedAt,
            comment: comment));
    }

    // ---------------------------------------------------------------------------
    // Private helpers
    // ---------------------------------------------------------------------------
    private static void EnsureTransitionIsAllowed(EventStatus from, EventStatus to)
    {
        if (!AllowedTransitions.TryGetValue(from, out var allowed) || !allowed.Contains(to))
        {
            throw new InvalidEventStatusTransitionException(from, to);
        }
    }
}
