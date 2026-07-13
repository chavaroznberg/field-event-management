using FieldEvents.Application.Commands;
using FieldEvents.Application.DTOs;
using FieldEvents.Application.Interfaces;
using FieldEvents.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace FieldEvents.Api.Controllers;

[ApiController]
[Route("api/events")]
public sealed class EventsController : ControllerBase
{
    private readonly IIngestEventUseCase _ingestEvent;
    private readonly IGetRecentEventsUseCase _getRecentEvents;
    private readonly IEventNotificationService _notify;

    public EventsController(
        IIngestEventUseCase ingestEvent,
        IGetRecentEventsUseCase getRecentEvents,
        IEventNotificationService notify)
    {
        _ingestEvent = ingestEvent;
        _getRecentEvents = getRecentEvents;
        _notify = notify;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<IngestEventResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecent(CancellationToken ct)
    {
        var events = await _getRecentEvents.ExecuteAsync(ct);
        return Ok(events);
    }

    /// <summary>
    /// Ingest a field event forwarded by the Agent.
    /// Returns 201 Created on first receipt, 200 OK for duplicate (idempotent retry).
    /// </summary>
    [HttpPost("ingest")]
    [ProducesResponseType(typeof(IngestEventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(IngestEventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Ingest(
        [FromBody] IngestEventRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<EventPriority>(request.Priority, ignoreCase: true, out var priority))
        {
            ModelState.AddModelError(
                nameof(request.Priority),
                $"'{request.Priority}' is not a valid priority. " +
                $"Valid values: {string.Join(", ", Enum.GetNames<EventPriority>())}");
            return ValidationProblem(ModelState);
        }

        var occurredAt = request.OccurredAt == default
            ? DateTimeOffset.UtcNow
            : request.OccurredAt;

        var command = new IngestEventCommand(
            request.ExternalEventId,
            request.SourceId,
            request.Title,
            request.Description,
            request.Location,
            priority,
            occurredAt);

        var (ev, wasCreated) = await _ingestEvent.ExecuteAsync(command, ct);

        if (wasCreated)
            await _notify.NotifyEventCreatedAsync(ev, ct);

        return wasCreated
            ? CreatedAtAction(nameof(Ingest), value: ev)
            : Ok(ev);
    }
}
