using FieldEvents.Api.Hubs;
using FieldEvents.Application.DTOs;
using FieldEvents.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace FieldEvents.Api.Notifications;

public sealed class SignalREventNotificationService : IEventNotificationService
{
    private readonly IHubContext<DispatcherHub> _hub;

    public SignalREventNotificationService(IHubContext<DispatcherHub> hub) => _hub = hub;

    public Task NotifyEventCreatedAsync(IngestEventResponse ev, CancellationToken ct = default)
        => _hub.Clients.All.SendAsync("EventCreated", ev, ct);
}
