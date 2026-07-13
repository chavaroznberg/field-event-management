# Field Event Management System

A take-home assignment demonstrating clean architecture, real-time communication,
reliability, security, and testability using .NET 10 and Angular 19.

---

## System overview

| Component | Technology | Port | Responsibility |
|---|---|---|---|
| **Agent** | ASP.NET Core 10 Minimal API | 5033 | Accepts events from external sources, queues durably in SQLite, forwards to Backend with exponential-backoff retry |
| **Backend** | ASP.NET Core 10 Web API | 5050 | Stores events in SQL Server, enforces idempotency, broadcasts real-time notifications via SignalR |
| **Frontend** | Angular 19 SPA | 4200 | Dispatcher view with live event table; skeleton Technician route |

> **SDK note:** The assignment specifies .NET 8. Only .NET SDK 10.0.103 is installed on the
> development machine. All code targets `net10.0`. No .NET-10-exclusive features are used.

---

## Repository structure

```
field-event-management/
├── src/
│   ├── backend/
│   │   ├── FieldEvents.Api/            Controllers, SignalR Hub, DI root
│   │   ├── FieldEvents.Application/    Use-case interfaces, commands, queries, DTOs
│   │   ├── FieldEvents.Domain/         Aggregate, state machine, domain exceptions
│   │   └── FieldEvents.Infrastructure/ EF Core, SQL Server, WebPush stub
│   ├── agent/
│   │   └── FieldEvents.Agent/          Minimal API, SQLite outbox, ForwardingWorker
│   └── frontend/
│       └── field-events-ui/            Angular application
├── tests/
│   ├── FieldEvents.Domain.Tests/       36 state-machine unit tests
│   ├── FieldEvents.IntegrationTests/   9 ingestion integration tests (SQLite)
│   └── FieldEvents.Agent.Tests/        9 auth unit tests
├── docs/
│   ├── architecture.md                 Component design, trade-offs, alternatives
│   ├── data-model.md                   Table definitions, ERD
│   ├── state-machine.md                States, transitions, enforcement
│   └── offline-pwa-design.md           PWA/offline design (Service Worker, IndexedDB, sync)
├── docker-compose.yml                  SQL Server container
├── Directory.Build.props               Shared compiler settings (Nullable, ImplicitUsings)
├── FieldEvents.slnx                    .NET 10 solution file
└── README.md
```

---

## Prerequisites

| Tool | Version | Purpose |
|---|---|---|
| [.NET SDK 10.x](https://dotnet.microsoft.com/download) | 10.0.103+ | Backend, Agent, tests |
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | any | SQL Server |
| [Node.js](https://nodejs.org/) | 20+ | Angular |
| [Angular CLI](https://angular.io/cli) | 19+ | Angular dev server + build |
| [dotnet-ef](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) | 10.x | Database migrations |

Install the CLI tools:
```bash
npm install -g @angular/cli
dotnet tool install -g dotnet-ef
```

---

## Quickstart

### 1. Start SQL Server

```bash
# Create .env with the SA password (not committed):
echo "SQL_SA_PASSWORD=YourStrong@Passw0rd1" > .env

docker compose up -d sqlserver
# Wait until: docker compose ps  →  health: healthy
```

### 2. Apply the database migration

```bash
dotnet ef database update \
  --project src/backend/FieldEvents.Infrastructure \
  --startup-project src/backend/FieldEvents.Infrastructure
```

### 3. Configure the Backend connection string

```bash
dotnet user-secrets set \
  "ConnectionStrings:DefaultConnection" \
  "Server=localhost,1433;Database=FieldEventsDb;User Id=sa;Password=YourStrong@Passw0rd1;TrustServerCertificate=True" \
  --project src/backend/FieldEvents.Api
```

### 4. Start the Backend

```bash
dotnet run --project src/backend/FieldEvents.Api
# Listening on http://localhost:5050
```

### 5. Start the Agent

```bash
dotnet run --project src/agent/FieldEvents.Agent
# Listening on http://localhost:5033
# ForwardingWorker polls every 5 s and forwards to http://localhost:5050
```

### 6. Start the Angular frontend

```bash
cd src/frontend/field-events-ui
npm install          # first time only
ng serve
# Open http://localhost:4200  →  connection badge shows "Connected"
```

---

## End-to-end demo

With all three processes running, send an event through the Agent:

**PowerShell:**
```powershell
$body = @{
    externalEventId = "DEMO-001"
    title           = "Pressure anomaly detected"
    description     = "Sensor reading exceeded threshold"
    location        = "Zone A, Pipe 7"
    priority        = "High"
    occurredAt      = "2026-07-13T10:00:00Z"
} | ConvertTo-Json

Invoke-WebRequest `
  -Uri http://localhost:5033/api/agent/events `
  -Method POST `
  -Body $body `
  -ContentType "application/json" `
  -Headers @{ "X-Source-Id" = "sensor-01"; "X-Api-Key" = "demo-api-key-sensor-01" }
```

**curl:**
```bash
curl -X POST http://localhost:5033/api/agent/events \
  -H "Content-Type: application/json" \
  -H "X-Source-Id: sensor-01" \
  -H "X-Api-Key: demo-api-key-sensor-01" \
  -d '{"externalEventId":"DEMO-001","title":"Pressure anomaly","location":"Zone A","priority":"High","occurredAt":"2026-07-13T10:00:00Z"}'
```

**Expected result:**
1. Agent returns `202 Accepted` immediately.
2. `ForwardingWorker` delivers to Backend within ~5 seconds.
3. Backend persists to SQL Server and broadcasts `EventCreated` via SignalR.
4. Angular table updates in real time (no page refresh).

**Demo credentials:**
- Source ID: `sensor-01`
- API key: `demo-api-key-sensor-01`
- SHA-256 hash stored in Agent config: `2d7dd118e6ed52e02d4e4fc0916c5aeffdac9965e5300c4b819bbb207c59b8cd`

---

## How to run tests

```bash
# All 54 tests (no database required — integration tests use SQLite in-memory)
dotnet test

# Individual test projects
dotnet test tests/FieldEvents.Domain.Tests        # 36 state machine unit tests
dotnet test tests/FieldEvents.IntegrationTests    # 9 ingestion + idempotency tests
dotnet test tests/FieldEvents.Agent.Tests         # 9 auth unit tests
```

---

## How to build Angular

```bash
cd src/frontend/field-events-ui
ng build --configuration=production
# Output: dist/field-events-ui/
```

---

## How to configure secrets

**Never commit real secrets.** All sensitive values are supplied via:

1. **User Secrets** (development):
   ```bash
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<connection-string>" \
     --project src/backend/FieldEvents.Api
   ```

2. **Environment variables** (CI / production):
   ```
   ConnectionStrings__DefaultConnection="Server=...;..."
   SQL_SA_PASSWORD="..."
   AGENT__BACKENDBASEURL="http://backend-service:5050"
   ```

The Agent's `appsettings.json` contains a demo SHA-256 hash of a development API key,
not a real secret. Do not use `demo-api-key-sensor-01` in production.

---

## Known limitations

| Limitation | Detail |
|---|---|
| **No JWT authentication** | The ingest endpoint is unauthenticated at the Backend level; `Users` and `SourceSystems` tables are migrated but login/token issuance is not implemented |
| **SQL Server required** | Docker not installed on the development machine; tests use SQLite instead |
| **SQLitePCLRaw CVE** | `GHSA-2m69-gcr7-jv3q` — transitive dependency of `Microsoft.EntityFrameworkCore.Sqlite` (test + Agent only); not in production SQL Server output |
| **SignalR notification gap** | The `EventCreated` push happens after `SaveChangesAsync`, not inside the same transaction. A process crash between the two means clients miss the notification. Clients recover on next page load. |
| **Web Push stub** | `WebPushNotificationService` logs but does not send real push notifications; VAPID keys and browser subscriptions are not implemented |
| **Skeleton use cases** | `IAssignEventUseCase`, `ITransferEventUseCase`, `IChangeEventStatusUseCase`, `ICloseEventUseCase`, `IAddEventCommentUseCase`, `IGetTechnicianEventsUseCase` are defined as interfaces only; no implementations or controllers are wired |
| **ForwardingWorker client-side filter** | EF Core SQLite cannot translate `DateTimeOffset` comparisons; the worker loads all Pending rows and filters `NextRetryAt` in memory (acceptable: queue is small) |

---

## Future improvements

- Full JWT authentication and role-based authorization
- Transactional outbox for SignalR to eliminate the notification gap
- Complete technician workflow (assign, status change, comments)
- Full Web Push implementation with VAPID keys
- Replace SQLite Agent queue with Azure Service Bus for horizontal scaling
- OpenTelemetry distributed tracing (Agent → Backend)
- Health check endpoints (`/health/ready`, `/health/live`) for Kubernetes
- HMAC request signing to prevent replay attacks on the ingest endpoint
- PWA support with Angular Service Worker and offline command queue
