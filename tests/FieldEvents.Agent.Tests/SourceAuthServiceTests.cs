using FieldEvents.Agent.Auth;

namespace FieldEvents.Agent.Tests;

public sealed class SourceAuthServiceTests
{
    private const string ValidSourceId = "sensor-01";
    private const string ValidApiKey   = "demo-api-key-sensor-01";

    // SHA-256 of "demo-api-key-sensor-01"
    private static readonly string ValidHash =
        SourceAuthService.ComputeSha256Hex(ValidApiKey);

    private static SourceAuthService BuildService(params SourceConfig[] sources)
        => new(sources);

    private static SourceAuthService DefaultService() => BuildService(
        new SourceConfig { SourceId = ValidSourceId, ApiKeyHash = ValidHash });

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public void Authenticate_ValidCredentials_ReturnsTrue()
    {
        var sut = DefaultService();
        Assert.True(sut.Authenticate(ValidSourceId, ValidApiKey));
    }

    [Fact]
    public void Authenticate_IsCaseInsensitive_ForStoredHash()
    {
        var sut = BuildService(new SourceConfig
        {
            SourceId = ValidSourceId,
            ApiKeyHash = ValidHash.ToUpperInvariant()
        });
        Assert.True(sut.Authenticate(ValidSourceId, ValidApiKey));
    }

    // ── Wrong credentials ─────────────────────────────────────────────────────

    [Fact]
    public void Authenticate_WrongApiKey_ReturnsFalse()
    {
        var sut = DefaultService();
        Assert.False(sut.Authenticate(ValidSourceId, "wrong-key"));
    }

    [Fact]
    public void Authenticate_UnknownSourceId_ReturnsFalse()
    {
        var sut = DefaultService();
        Assert.False(sut.Authenticate("unknown-sensor", ValidApiKey));
    }

    [Fact]
    public void Authenticate_EmptySourceId_ReturnsFalse()
    {
        var sut = DefaultService();
        Assert.False(sut.Authenticate("", ValidApiKey));
    }

    [Fact]
    public void Authenticate_EmptyApiKey_ReturnsFalse()
    {
        var sut = DefaultService();
        Assert.False(sut.Authenticate(ValidSourceId, ""));
    }

    [Fact]
    public void Authenticate_NoSourcesConfigured_ReturnsFalse()
    {
        var sut = BuildService();
        Assert.False(sut.Authenticate(ValidSourceId, ValidApiKey));
    }

    // ── Hash utility ──────────────────────────────────────────────────────────

    [Fact]
    public void ComputeSha256Hex_IsAlwaysLowercase64Chars()
    {
        var hash = SourceAuthService.ComputeSha256Hex("any-input");
        Assert.Equal(64, hash.Length);
        Assert.Equal(hash, hash.ToLowerInvariant());
    }

    [Fact]
    public void ComputeSha256Hex_DifferentInputsProduceDifferentHashes()
    {
        var h1 = SourceAuthService.ComputeSha256Hex("key-a");
        var h2 = SourceAuthService.ComputeSha256Hex("key-b");
        Assert.NotEqual(h1, h2);
    }
}
