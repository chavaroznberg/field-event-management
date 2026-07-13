using System.Security.Cryptography;
using System.Text;

namespace FieldEvents.Agent.Auth;

public sealed class SourceAuthService
{
    private readonly IReadOnlyList<SourceConfig> _sources;

    public SourceAuthService(IEnumerable<SourceConfig> sources)
        => _sources = [.. sources];

    /// <summary>
    /// Returns true when sourceId is registered and apiKey hashes to the stored value.
    /// Uses constant-time comparison to prevent timing oracle attacks.
    /// </summary>
    public bool Authenticate(string sourceId, string apiKey)
    {
        if (string.IsNullOrEmpty(sourceId) || string.IsNullOrEmpty(apiKey))
            return false;

        var source = _sources.FirstOrDefault(s =>
            string.Equals(s.SourceId, sourceId, StringComparison.Ordinal));

        if (source is null)
            return false;

        var providedHash = ComputeSha256Hex(apiKey);

        // FixedTimeEquals requires equal-length spans; both are 64-char hex strings.
        var a = Encoding.UTF8.GetBytes(providedHash);
        var b = Encoding.UTF8.GetBytes(source.ApiKeyHash.ToLowerInvariant());
        return CryptographicOperations.FixedTimeEquals(a, b);
    }

    public static string ComputeSha256Hex(string input)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
