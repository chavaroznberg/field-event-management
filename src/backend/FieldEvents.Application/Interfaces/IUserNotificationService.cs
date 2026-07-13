using FieldEvents.Application.DTOs;

namespace FieldEvents.Application.Interfaces;

/// <summary>
/// Push notifications to individual users (technicians) on their subscribed devices.
/// Distinct from IEventNotificationService which broadcasts to all Dispatcher clients via SignalR.
/// </summary>
public interface IUserNotificationService
{
    Task NotifyAssignedAsync(Guid userId, IngestEventResponse ev, CancellationToken ct = default);
    Task NotifyStatusChangedAsync(Guid userId, IngestEventResponse ev, CancellationToken ct = default);
}
