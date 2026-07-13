namespace FieldEvents.Agent.Auth;

/// <summary>A single authorised source system, loaded from configuration.</summary>
public sealed class SourceConfig
{
    public string SourceId { get; init; } = string.Empty;

    /// <summary>SHA-256 hex digest of the plaintext API key. Never store the raw key.</summary>
    public string ApiKeyHash { get; init; } = string.Empty;
}
