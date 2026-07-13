using FieldEvents.Application.DTOs;

namespace FieldEvents.Application.Interfaces;

public interface IEventNotificationService
{
    Task NotifyEventCreatedAsync(IngestEventResponse ev, CancellationToken ct = default);
}
