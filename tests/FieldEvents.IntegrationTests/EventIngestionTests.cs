using System.Net;
using System.Net.Http.Json;
using FieldEvents.Application.DTOs;

namespace FieldEvents.IntegrationTests;

public sealed class EventIngestionTests : IClassFixture<FieldEventsWebApplicationFactory>
{
    private readonly FieldEventsWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public EventIngestionTests(FieldEventsWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static IngestEventRequest NewRequest(string? externalId = null) => new()
    {
        ExternalEventId = externalId ?? $"EXT-{Guid.NewGuid():N}",
        SourceId = "sensor-01",
        Title = "Pressure anomaly detected",
        Description = "Reading exceeded safe threshold",
        Location = "Zone A, Pipe 7",
        Priority = "High",
        OccurredAt = DateTimeOffset.UtcNow.AddMinutes(-5),
    };

    // ---------------------------------------------------------------------------
    // Happy path
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task Post_NewEvent_Returns201_WithCorrectBody()
    {
        var response = await _client.PostAsJsonAsync("/api/events/ingest", NewRequest());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<IngestEventResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.Id);
        Assert.Equal("sensor-01", body.SourceId);
        Assert.Equal("New", body.Status);
        Assert.Equal("High", body.Priority);
    }

    [Fact]
    public async Task Post_NewEvent_PersistsExactlyOneRow()
    {
        var request = NewRequest($"PERSIST-{Guid.NewGuid():N}");

        var response = await _client.PostAsJsonAsync("/api/events/ingest", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var count = await _factory.CountEventsAsync(request.SourceId, request.ExternalEventId);
        Assert.Equal(1, count);
    }

    // ---------------------------------------------------------------------------
    // Idempotency
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task Post_DuplicateEvent_SecondCallReturns200()
    {
        var request = NewRequest($"DUP-200-{Guid.NewGuid():N}");

        var first = await _client.PostAsJsonAsync("/api/events/ingest", request);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await _client.PostAsJsonAsync("/api/events/ingest", request);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
    }

    [Fact]
    public async Task Post_DuplicateEvent_BothCallsReturnSameId()
    {
        var request = NewRequest($"DUP-ID-{Guid.NewGuid():N}");

        var first = await _client.PostAsJsonAsync("/api/events/ingest", request);
        var second = await _client.PostAsJsonAsync("/api/events/ingest", request);

        var b1 = await first.Content.ReadFromJsonAsync<IngestEventResponse>();
        var b2 = await second.Content.ReadFromJsonAsync<IngestEventResponse>();

        Assert.Equal(b1!.Id, b2!.Id);
    }

    [Fact]
    public async Task Post_DuplicateEvent_StoredOnce_In_Database()
    {
        var request = NewRequest($"DUP-DB-{Guid.NewGuid():N}");

        // Send the same event three times.
        await _client.PostAsJsonAsync("/api/events/ingest", request);
        await _client.PostAsJsonAsync("/api/events/ingest", request);
        await _client.PostAsJsonAsync("/api/events/ingest", request);

        var count = await _factory.CountEventsAsync(request.SourceId, request.ExternalEventId);
        Assert.Equal(1, count);
    }

    // ---------------------------------------------------------------------------
    // Validation
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task Post_InvalidPriority_Returns400()
    {
        var request = NewRequest() with { Priority = "SuperUrgent" };

        var response = await _client.PostAsJsonAsync("/api/events/ingest", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_EmptyTitle_Returns400()
    {
        var request = NewRequest() with { Title = "" };

        var response = await _client.PostAsJsonAsync("/api/events/ingest", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_MissingExternalEventId_Returns400()
    {
        var request = NewRequest() with { ExternalEventId = "" };

        var response = await _client.PostAsJsonAsync("/api/events/ingest", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
