namespace FieldEvents.Agent.Queue;

public sealed class OutboxMessage
{
    private OutboxMessage() { }

    public Guid Id { get; private set; }
    public string SourceId { get; private set; } = string.Empty;
    public string ExternalEventId { get; private set; } = string.Empty;

    /// <summary>Composite key (SourceId:ExternalEventId) used for deduplication in the agent queue.</summary>
    public string IdempotencyKey { get; private set; } = string.Empty;

    /// <summary>JSON payload in the format expected by Backend POST /api/events/ingest.</summary>
    public string Payload { get; private set; } = string.Empty;

    public OutboxStatus Status { get; set; }
    public int RetryCount { get; set; }

    /// <summary>
    /// When the worker should next attempt forwarding.
    /// DateTimeOffset.MinValue means "process immediately" — avoids a nullable
    /// type that EF Core SQLite cannot translate in WHERE clauses.
    /// </summary>
    public DateTimeOffset NextRetryAt { get; set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? DeliveredAt { get; set; }
    public string? LastError { get; set; }

    public static OutboxMessage Create(string sourceId, string externalEventId, string payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(externalEventId);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            SourceId = sourceId,
            ExternalEventId = externalEventId,
            IdempotencyKey = $"{sourceId}:{externalEventId}",
            Payload = payload,
            Status = OutboxStatus.Pending,
            RetryCount = 0,
            NextRetryAt = DateTimeOffset.MinValue, // process immediately
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }
}
