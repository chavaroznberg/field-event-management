namespace FieldEvents.Infrastructure.Persistence.Entities;

/// <summary>
/// Represents an external system that is authorised to report events to the Agent.
/// API keys are stored as SHA-256 hashes — never as plaintext.
/// </summary>
public sealed class SourceSystem
{
    private SourceSystem() { }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;

    /// <summary>SHA-256 hex digest of the plaintext API key.</summary>
    public string ApiKeyHash { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static SourceSystem Create(string name, string apiKeyHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKeyHash);

        return new SourceSystem
        {
            Id = Guid.NewGuid(),
            Name = name,
            ApiKeyHash = apiKeyHash,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }
}
