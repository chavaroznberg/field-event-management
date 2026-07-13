using System.Text.Json;
using FieldEvents.Agent.Auth;
using FieldEvents.Agent.Models;
using FieldEvents.Agent.Persistence;
using FieldEvents.Agent.Queue;
using FieldEvents.Agent.Workers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Logging ──────────────────────────────────────────────────────────────────
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// ── Source authentication ─────────────────────────────────────────────────────
var sources = builder.Configuration
    .GetSection("Agent:Sources")
    .Get<List<SourceConfig>>() ?? [];

builder.Services.AddSingleton(new SourceAuthService(sources));

// ── SQLite queue ──────────────────────────────────────────────────────────────
var dbPath = builder.Configuration.GetConnectionString("AgentDb")
    ?? "DataSource=Data/agent-queue.db";

// Ensure the Data directory exists before EF Core opens the file.
var dataDir = Path.GetDirectoryName(dbPath.Replace("DataSource=", "").Split(';')[0]);
if (!string.IsNullOrEmpty(dataDir))
    Directory.CreateDirectory(dataDir);

builder.Services.AddDbContext<AgentDbContext>(options =>
    options.UseSqlite(dbPath));

// ── HTTP client for Backend ───────────────────────────────────────────────────
var backendUrl = builder.Configuration["Agent:BackendBaseUrl"]
    ?? throw new InvalidOperationException("Agent:BackendBaseUrl is required.");

builder.Services.AddHttpClient("Backend", client =>
{
    client.BaseAddress = new Uri(backendUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// ── Background forwarding worker ──────────────────────────────────────────────
builder.Services.AddHostedService<ForwardingWorker>();

builder.Services.AddProblemDetails();

var app = builder.Build();

// Create schema on first run (no migrations needed for a local queue DB).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AgentDbContext>();
    db.Database.EnsureCreated();
}

app.UseExceptionHandler();
app.UseStatusCodePages();

// ── Endpoints ─────────────────────────────────────────────────────────────────

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

app.MapPost("/api/agent/events", async (
    HttpContext ctx,
    [FromBody] AgentEventRequest request,
    AgentDbContext db,
    SourceAuthService auth,
    ILogger<Program> logger,
    CancellationToken ct) =>
{
    // 1. Extract source credentials from headers.
    var sourceId = ctx.Request.Headers["X-Source-Id"].FirstOrDefault();
    var apiKey   = ctx.Request.Headers["X-Api-Key"].FirstOrDefault();

    if (string.IsNullOrEmpty(sourceId) || string.IsNullOrEmpty(apiKey))
        return Results.Problem("X-Source-Id and X-Api-Key headers are required.",
            statusCode: 401, title: "Unauthorised");

    // 2. Authenticate.
    if (!auth.Authenticate(sourceId, apiKey))
    {
        logger.LogWarning("Authentication failed for SourceId={SourceId}", sourceId);
        return Results.Problem("Invalid source credentials.",
            statusCode: 401, title: "Unauthorised");
    }

    // 3. Validate OccurredAt; default to UtcNow if omitted.
    var occurredAt = request.OccurredAt == default
        ? DateTimeOffset.UtcNow
        : request.OccurredAt;

    // 4. Build the payload in the exact shape Backend expects.
    var backendPayload = JsonSerializer.Serialize(new
    {
        externalEventId = request.ExternalEventId,
        sourceId        = sourceId,
        title           = request.Title,
        description     = request.Description,
        location        = request.Location,
        priority        = request.Priority,
        occurredAt      = occurredAt,
    });

    // 5. Persist to SQLite queue — idempotent: skip if already received.
    var idempotencyKey = $"{sourceId}:{request.ExternalEventId}";
    var exists = await db.OutboxMessages
        .AnyAsync(m => m.IdempotencyKey == idempotencyKey, ct);

    if (exists)
    {
        logger.LogInformation(
            "Duplicate event received: SourceId={SourceId} ExternalEventId={ExternalEventId} — skipped",
            sourceId, request.ExternalEventId);
        return Results.Accepted();
    }

    var message = OutboxMessage.Create(sourceId, request.ExternalEventId, backendPayload);
    db.OutboxMessages.Add(message);

    try
    {
        await db.SaveChangesAsync(ct);
    }
    catch (DbUpdateException)
    {
        // Race between AnyAsync check and SaveChanges (same idempotency key).
        // Treat as a duplicate — still return 202.
        logger.LogDebug("Idempotency race for key {Key} — treated as duplicate", idempotencyKey);
        return Results.Accepted();
    }

    logger.LogInformation(
        "Queued event {Id}: SourceId={SourceId} ExternalEventId={ExternalEventId}",
        message.Id, sourceId, request.ExternalEventId);

    return Results.Accepted();
})
.WithName("IngestAgentEvent")
.Accepts<AgentEventRequest>("application/json")
.Produces(202)
.ProducesProblem(401)
.ProducesProblem(400);

app.Run();

public partial class Program { }
