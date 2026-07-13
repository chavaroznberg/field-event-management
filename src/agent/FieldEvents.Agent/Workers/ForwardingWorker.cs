using FieldEvents.Agent.Persistence;
using FieldEvents.Agent.Queue;
using Microsoft.EntityFrameworkCore;

namespace FieldEvents.Agent.Workers;

public sealed class ForwardingWorker : BackgroundService
{
    // Exponential backoff schedule (index = retry count).
    private static readonly TimeSpan[] Backoffs =
    [
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(15),
        TimeSpan.FromSeconds(30),
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(2),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(10),
        TimeSpan.FromMinutes(15),
        TimeSpan.FromMinutes(30),
        TimeSpan.FromMinutes(30),
    ];

    private const int MaxRetries = 10;
    private const int BatchSize = 10;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ForwardingWorker> _logger;

    public ForwardingWorker(
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory,
        ILogger<ForwardingWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("ForwardingWorker started");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(ct);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                _logger.LogError(ex, "Unhandled error in ForwardingWorker batch");
            }

            try { await Task.Delay(PollInterval, ct); }
            catch (OperationCanceledException) { break; }
        }

        _logger.LogInformation("ForwardingWorker stopped");
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AgentDbContext>();

        var now = DateTimeOffset.UtcNow;

        // EF Core SQLite cannot translate DateTimeOffset comparisons in SQL.
        // The agent queue is small, so filtering NextRetryAt client-side is acceptable.
        var candidates = await db.OutboxMessages
            .Where(m => m.Status == OutboxStatus.Pending && m.RetryCount < MaxRetries)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(ct);

        var pending = candidates
            .Where(m => m.NextRetryAt <= now)
            .Take(BatchSize)
            .ToList();

        if (pending.Count == 0) return;

        _logger.LogDebug("Processing {Count} pending outbox messages", pending.Count);

        var client = _httpClientFactory.CreateClient("Backend");

        foreach (var msg in pending)
        {
            if (ct.IsCancellationRequested) break;
            await ForwardMessageAsync(db, client, msg, ct);
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task ForwardMessageAsync(
        AgentDbContext db, HttpClient client, OutboxMessage msg, CancellationToken ct)
    {
        try
        {
            using var content = new StringContent(
                msg.Payload,
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync("/api/events/ingest", content, ct);

            if (response.StatusCode is System.Net.HttpStatusCode.OK
                                    or System.Net.HttpStatusCode.Created)
            {
                msg.Status = OutboxStatus.Delivered;
                msg.DeliveredAt = DateTimeOffset.UtcNow;
                msg.LastError = null;
                _logger.LogInformation(
                    "Delivered outbox message {Id} (ExternalEventId={ExternalEventId})",
                    msg.Id, msg.ExternalEventId);
            }
            else
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                ScheduleRetry(msg, $"HTTP {(int)response.StatusCode}: {body[..Math.Min(200, body.Length)]}");
                _logger.LogWarning(
                    "Backend returned {StatusCode} for message {Id}; scheduled retry {RetryCount}",
                    response.StatusCode, msg.Id, msg.RetryCount);
            }
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            ScheduleRetry(msg, ex.Message);
            _logger.LogWarning(
                "Failed to forward message {Id}: {Error}; scheduled retry {RetryCount}",
                msg.Id, ex.Message, msg.RetryCount);
        }
    }

    private static void ScheduleRetry(OutboxMessage msg, string error)
    {
        msg.RetryCount++;
        msg.LastError = error;
        var delay = msg.RetryCount < Backoffs.Length
            ? Backoffs[msg.RetryCount - 1]
            : Backoffs[^1];
        msg.NextRetryAt = DateTimeOffset.UtcNow.Add(delay);
    }
}
