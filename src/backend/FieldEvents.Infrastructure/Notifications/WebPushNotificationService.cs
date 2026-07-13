using FieldEvents.Application.DTOs;
using FieldEvents.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace FieldEvents.Infrastructure.Notifications;

/// <summary>
/// Stub implementation of IUserNotificationService.
/// Real implementation would require:
///   - VAPID key pair (private key stored in secrets, public key sent to browser)
///   - PushSubscription records per user stored in the database
///   - A Web Push library (e.g. WebPush or Lib.AspNetCore.WebPush)
///   - Browser-side service worker to receive and display notifications
/// </summary>
public sealed class WebPushNotificationService : IUserNotificationService
{
    private readonly ILogger<WebPushNotificationService> _logger;

    public WebPushNotificationService(ILogger<WebPushNotificationService> logger) => _logger = logger;

    public Task NotifyAssignedAsync(Guid userId, IngestEventResponse ev, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[WebPush stub] Would push assignment notification to user {UserId} for event '{Title}' ({EventId})",
            userId, ev.Title, ev.Id);
        return Task.CompletedTask;
    }

    public Task NotifyStatusChangedAsync(Guid userId, IngestEventResponse ev, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[WebPush stub] Would push status-change notification to user {UserId} — event '{Title}' is now {Status}",
            userId, ev.Title, ev.Status);
        return Task.CompletedTask;
    }
}
