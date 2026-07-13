using FieldEvents.Domain.Aggregates;
using FieldEvents.Domain.Enums;
using FieldEvents.Domain.Exceptions;

namespace FieldEvents.Domain.Tests;

public class FieldEventStateMachineTests
{
    // Fixed user IDs make test failures easier to read — we can see which user was expected.
    private static readonly Guid SystemUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid DispatcherId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    // ---------------------------------------------------------------------------
    // Helper: standard event used in most tests
    // ---------------------------------------------------------------------------
    private static FieldEvent CreateNewEvent() =>
        FieldEvent.Create(
            externalEventId: "EXT-001",
            sourceId: "sensor-17",
            title: "Pressure anomaly detected",
            description: null,
            location: "Zone A, Pipe 7",
            priority: EventPriority.High,
            occurredAt: DateTimeOffset.UtcNow.AddMinutes(-5),
            createdByUserId: SystemUserId);

    // Helper: drives a new event through the shortest valid path to reach a target status.
    private static FieldEvent CreateEventAtStatus(EventStatus target)
    {
        var ev = CreateNewEvent();
        foreach (var step in PathTo(target))
            ev.ChangeStatus(step, DispatcherId);
        return ev;
    }

    // Shortest valid paths from New to each status.
    private static IEnumerable<EventStatus> PathTo(EventStatus target) => target switch
    {
        EventStatus.New => [],
        EventStatus.Assigned => [EventStatus.Assigned],
        EventStatus.InProgress => [EventStatus.Assigned, EventStatus.InProgress],
        EventStatus.WaitingForInformation => [EventStatus.Assigned, EventStatus.InProgress, EventStatus.WaitingForInformation],
        EventStatus.Completed => [EventStatus.Assigned, EventStatus.InProgress, EventStatus.Completed],
        EventStatus.Cancelled => [EventStatus.Cancelled],
        _ => throw new ArgumentOutOfRangeException(nameof(target))
    };

    // ===========================================================================
    // Initial state
    // ===========================================================================

    [Fact]
    public void New_event_starts_in_New_status()
    {
        var ev = CreateNewEvent();

        Assert.Equal(EventStatus.New, ev.Status);
    }

    [Fact]
    public void Creating_an_event_records_exactly_one_initial_history_entry()
    {
        var ev = CreateNewEvent();

        Assert.Single(ev.StatusHistory);
    }

    [Fact]
    public void Initial_history_entry_has_null_previous_status()
    {
        var ev = CreateNewEvent();

        Assert.Null(ev.StatusHistory.Single().PreviousStatus);
    }

    [Fact]
    public void Initial_history_entry_records_New_as_the_new_status()
    {
        var ev = CreateNewEvent();

        Assert.Equal(EventStatus.New, ev.StatusHistory.Single().NewStatus);
    }

    [Fact]
    public void Initial_history_entry_records_the_user_who_created_the_event()
    {
        var ev = CreateNewEvent();

        Assert.Equal(SystemUserId, ev.StatusHistory.Single().ChangedByUserId);
    }

    [Fact]
    public void Initial_history_entry_has_a_UTC_timestamp_within_the_creation_window()
    {
        var before = DateTimeOffset.UtcNow;
        var ev = CreateNewEvent();
        var after = DateTimeOffset.UtcNow;

        var entry = ev.StatusHistory.Single();
        Assert.True(entry.ChangedAt >= before, "ChangedAt should not predate the test start.");
        Assert.True(entry.ChangedAt <= after, "ChangedAt should not postdate the test end.");
    }

    // ===========================================================================
    // Valid transitions — every allowed path must succeed
    // ===========================================================================

    [Theory]
    [InlineData(EventStatus.New,                    EventStatus.Assigned)]
    [InlineData(EventStatus.New,                    EventStatus.Cancelled)]
    [InlineData(EventStatus.Assigned,               EventStatus.New)]
    [InlineData(EventStatus.Assigned,               EventStatus.InProgress)]
    [InlineData(EventStatus.Assigned,               EventStatus.Cancelled)]
    [InlineData(EventStatus.InProgress,             EventStatus.WaitingForInformation)]
    [InlineData(EventStatus.InProgress,             EventStatus.Completed)]
    [InlineData(EventStatus.InProgress,             EventStatus.Cancelled)]
    [InlineData(EventStatus.WaitingForInformation,  EventStatus.InProgress)]
    [InlineData(EventStatus.WaitingForInformation,  EventStatus.Cancelled)]
    public void Valid_transition_succeeds_and_updates_status(EventStatus from, EventStatus to)
    {
        var ev = CreateEventAtStatus(from);

        ev.ChangeStatus(to, DispatcherId);

        Assert.Equal(to, ev.Status);
    }

    // ===========================================================================
    // Invalid transitions — every disallowed path must throw
    // ===========================================================================

    [Theory]
    [InlineData(EventStatus.New,                    EventStatus.InProgress)]
    [InlineData(EventStatus.New,                    EventStatus.WaitingForInformation)]
    [InlineData(EventStatus.New,                    EventStatus.Completed)]
    [InlineData(EventStatus.Assigned,               EventStatus.Completed)]
    [InlineData(EventStatus.Assigned,               EventStatus.WaitingForInformation)]
    [InlineData(EventStatus.WaitingForInformation,  EventStatus.New)]
    [InlineData(EventStatus.WaitingForInformation,  EventStatus.Assigned)]
    [InlineData(EventStatus.WaitingForInformation,  EventStatus.Completed)]
    public void Invalid_transition_throws_InvalidEventStatusTransitionException(EventStatus from, EventStatus to)
    {
        var ev = CreateEventAtStatus(from);

        Assert.Throws<InvalidEventStatusTransitionException>(
            () => ev.ChangeStatus(to, DispatcherId));
    }

    [Fact]
    public void Exception_carries_the_from_and_to_status_that_was_attempted()
    {
        var ev = CreateNewEvent(); // Status = New

        var ex = Assert.Throws<InvalidEventStatusTransitionException>(
            () => ev.ChangeStatus(EventStatus.Completed, DispatcherId));

        Assert.Equal(EventStatus.New, ex.From);
        Assert.Equal(EventStatus.Completed, ex.To);
    }

    // ===========================================================================
    // Terminal states — Completed and Cancelled allow no further transitions
    // ===========================================================================

    [Fact]
    public void Completed_is_a_terminal_state_and_rejects_every_transition()
    {
        var ev = CreateEventAtStatus(EventStatus.Completed);

        foreach (EventStatus target in Enum.GetValues<EventStatus>())
        {
            Assert.Throws<InvalidEventStatusTransitionException>(
                () => ev.ChangeStatus(target, DispatcherId));
        }
    }

    [Fact]
    public void Cancelled_is_a_terminal_state_and_rejects_every_transition()
    {
        var ev = CreateEventAtStatus(EventStatus.Cancelled);

        foreach (EventStatus target in Enum.GetValues<EventStatus>())
        {
            Assert.Throws<InvalidEventStatusTransitionException>(
                () => ev.ChangeStatus(target, DispatcherId));
        }
    }

    // ===========================================================================
    // History after successful transitions
    // ===========================================================================

    [Fact]
    public void Successful_transition_adds_exactly_one_history_entry()
    {
        var ev = CreateNewEvent();
        var countBefore = ev.StatusHistory.Count;

        ev.ChangeStatus(EventStatus.Assigned, DispatcherId);

        Assert.Equal(countBefore + 1, ev.StatusHistory.Count);
    }

    [Fact]
    public void History_entry_records_the_correct_previous_and_new_status()
    {
        var ev = CreateNewEvent();

        ev.ChangeStatus(EventStatus.Assigned, DispatcherId);

        var entry = ev.StatusHistory.Last();
        Assert.Equal(EventStatus.New, entry.PreviousStatus);
        Assert.Equal(EventStatus.Assigned, entry.NewStatus);
    }

    [Fact]
    public void History_entry_records_the_user_who_changed_the_status()
    {
        var ev = CreateNewEvent();

        ev.ChangeStatus(EventStatus.Assigned, DispatcherId);

        Assert.Equal(DispatcherId, ev.StatusHistory.Last().ChangedByUserId);
    }

    [Fact]
    public void History_entry_has_a_UTC_timestamp_within_the_transition_window()
    {
        var ev = CreateNewEvent();
        var before = DateTimeOffset.UtcNow;

        ev.ChangeStatus(EventStatus.Assigned, DispatcherId);

        var after = DateTimeOffset.UtcNow;
        var entry = ev.StatusHistory.Last();
        Assert.True(entry.ChangedAt >= before);
        Assert.True(entry.ChangedAt <= after);
    }

    [Fact]
    public void Multiple_transitions_accumulate_history_entries_in_order()
    {
        var ev = CreateNewEvent();

        ev.ChangeStatus(EventStatus.Assigned, DispatcherId);
        ev.ChangeStatus(EventStatus.InProgress, DispatcherId);
        ev.ChangeStatus(EventStatus.Completed, DispatcherId);

        // 1 initial + 3 transitions = 4 entries
        Assert.Equal(4, ev.StatusHistory.Count);

        var statuses = ev.StatusHistory.Select(h => h.NewStatus).ToList();
        Assert.Equal([EventStatus.New, EventStatus.Assigned, EventStatus.InProgress, EventStatus.Completed], statuses);
    }

    [Fact]
    public void Optional_comment_is_stored_in_the_history_entry()
    {
        var ev = CreateNewEvent();
        const string comment = "Assigning to technician on duty";

        ev.ChangeStatus(EventStatus.Assigned, DispatcherId, comment);

        Assert.Equal(comment, ev.StatusHistory.Last().Comment);
    }

    [Fact]
    public void History_entry_has_null_comment_when_none_is_provided()
    {
        var ev = CreateNewEvent();

        ev.ChangeStatus(EventStatus.Assigned, DispatcherId);

        Assert.Null(ev.StatusHistory.Last().Comment);
    }

    // ===========================================================================
    // Aggregate integrity — failed transitions must not modify the aggregate
    // ===========================================================================

    [Fact]
    public void Failed_transition_does_not_change_the_event_status()
    {
        var ev = CreateNewEvent(); // Status = New

        try { ev.ChangeStatus(EventStatus.InProgress, DispatcherId); }
        catch (InvalidEventStatusTransitionException) { /* expected */ }

        Assert.Equal(EventStatus.New, ev.Status);
    }

    [Fact]
    public void Failed_transition_does_not_add_a_history_entry()
    {
        var ev = CreateNewEvent();
        var countBefore = ev.StatusHistory.Count;

        try { ev.ChangeStatus(EventStatus.InProgress, DispatcherId); }
        catch (InvalidEventStatusTransitionException) { /* expected */ }

        Assert.Equal(countBefore, ev.StatusHistory.Count);
    }
}
