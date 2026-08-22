# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

NotificationService is a microservice in the **flow OverStack** platform (see sibling
services [UserService](https://github.com/flow-OverStack/UserService),
[QuestionService](https://github.com/flow-OverStack/QuestionService),
[AnswerService](https://github.com/flow-OverStack/AnswerService), also checked out one
directory above this repo). It consumes domain events from Kafka (votes, answers,
comments, etc. from the other services), persists them as `UserEvent`/notification
records, and pushes them to connected clients in real time over a SignalR hub. It exposes
a REST API (no GraphQL/gRPC, unlike the siblings) for reading/marking-read notification
history.

## Build / Run / Test

Targets **.NET 10** (`net10.0` in every `.csproj`; no `global.json` pinning the SDK).

```bash
# Build whole solution
dotnet build

# Run the API (REST + SignalR hub on the same Kestrel process)
cd NotificationService.Api
dotnet run
```

Tests are split into two xUnit categories via class-level `[UnitTest]` / `[FunctionalTest]`
trait attributes (`Traits/CategoryDiscoverer.cs`), naming convention
`Method_Scenario_ExpectedResult`:

```bash
cd NotificationService.Tests
dotnet test --filter Category=Unit          # fast, no external deps (Moq/MockQueryable)
dotnet test --filter Category=Functional    # requires Docker (Testcontainers)
dotnet test --filter "FullyQualifiedName~MarkAsRead"   # a single test/class
```

**Functional tests need a running Docker daemon** — `FunctionalTestWebAppFactory` spins up
PostgreSQL + Redis via Testcontainers and stubs Keycloak with WireMock. All six functional
test classes share one `[Collection(FunctionalTestCollection.Name)]` and run sequentially
against each other (Docker-backed state); unit tests still run in parallel.

`Program.cs` reads `RedisSettings`/`ConnectionStrings:PostgresSQL` eagerly, before
`builder.Build()` — `WebApplicationFactory.ConfigureWebHost`/`ConfigureAppConfiguration`
callbacks apply too late for these. `FunctionalTestWebAppFactory` works around this by
setting env vars (e.g. `RedisSettings__Host`) in `InitializeAsync()` *before* the host is
built. Keep this in mind if you add more config read before `Build()`.

### EF Core migrations

Migrations live in `NotificationService.DAL/Migrations`; the startup project
(`NotificationService.Api`) holds the EF Design package and the connection string.

```bash
dotnet ef migrations add <Name> --project NotificationService.DAL --startup-project NotificationService.Api
```

Migrations are **auto-applied on startup only when `ASPNETCORE_ENVIRONMENT=Development`**
(`MigrateDatabaseAsync` in `Program.cs`). In production, generate and apply a SQL script.

## Architecture

Clean Architecture, one project per layer (see `NotificationService.sln` solution folders):

| Layer | Projects |
|-------|----------|
| Domain | `NotificationService.Domain` — entities, DTOs, enums, interfaces, settings, the Result types. No external deps. |
| Application | `NotificationService.Application` — services (business logic), AutoMapper mappings, FluentValidation validators, localized error resources. |
| Infrastructure | `NotificationService.DAL` (EF Core/Postgres), `NotificationService.Cache` (Redis), `NotificationService.Messaging` (Kafka/MassTransit + Hangfire-backed redelivery). |
| Presentation | `NotificationService.Api` (REST + SignalR hub + composition root). |

Each non-Domain project owns a `DependencyInjection/DependencyInjection.cs` with an
`Add<Layer>()` extension; `Program.cs` wires them together, and `Startup.cs` holds the
cross-cutting `IServiceCollection`/`WebApplication` extensions (auth, SignalR/Redis
backplane, Swagger, Hangfire, OpenTelemetry, CORS, health checks).

### Key patterns — internalize these before editing

- **Result pattern, not exceptions for business outcomes.** Services return
  `BaseResult` / `BaseResult<T>` / `CollectionResult<T>` (`NotificationService.Domain/Results`).
  Success/failure is data; `ErrorMessage` + `ErrorCode` carry failures. Controllers
  translate via `ToActionResult` (`Api/Extensions/BaseResultExtensions.cs`), which maps
  `ErrorCodes` to HTTP status codes through a static dictionary. `ErrorCodes` enum +
  localized `ErrorMessage.resx` (`en`, `ru-by`) are the source of error identity.

- **Caching = Decorator pattern, registered explicitly (no Scrutor here).**
  `NotificationService.cs` implements both `INotificationService` and
  `INotificationEventHandler`; `CacheNotificationService` / `CacheNotificationEventHandler`
  wrap them and are wired with `services.Decorate<TInterface, TDecorator>()` in
  `Application/DependencyInjection`. Reads (`GetAllByRecipientIdAsync`) check
  `INotificationCacheRepository.GetAsync` before hitting `inner`, then `SetAsync` on miss;
  writes (`CreateAsync`, `MarkAsReadAsync`) call `InvalidateAsync` after a successful
  `inner` call. **To add a cached read/write path, add a `Cache*` decorator and register it
  explicitly — don't put cache logic in the base service, and don't forget the explicit
  `Decorate` call (there's no assembly-scan auto-registration in this service).**

- **Data access:** thin `IBaseRepository<T>` / `BaseRepository<T>` over EF Core
  (`NotificationService.DAL`), one entity (`UserEvent`). No Unit-of-Work abstraction —
  repository exposes `SaveChangesAsync` directly.

- **Messaging is retry-then-dead-letter, not idempotency-deduped.** Kafka events
  (`BaseEvent`) consumed via MassTransit's Kafka rider (`UserEventConsumer`).
  `ResilientConsumeFilter<TEvent>` wraps every consume: a few immediate in-process retries,
  then Hangfire-scheduled redelivery with increasing backoff (1m → 24h) via
  `RedeliveryJob.PublishWithRedelivery`, then move-to-dead-letter (`FaultedMessage`
  produced to `KafkaSettings.DeadLetterTopic`) once retries are exhausted. A `KillSwitch`
  trips consumption if failures spike. Duplicate-event protection is instead enforced in
  `NotificationService.CreateAsync` by checking `EventId` uniqueness before insert.

- **Realtime push best-effort, never blocks the write.** `NotificationService.CreateAsync`
  persists first, then calls `INotificationPusher.PushAsync` (SignalR,
  `SignalRNotificationPusher` → `NotificationHub`) inside a try/catch that swallows all
  exceptions — an offline/unreachable client must never fail notification creation.
  SignalR uses a Redis backplane (`AddStackExchangeRedis`) so pushes fan out across
  instances.

- **Auth:** JWT Bearer validated against Keycloak (`MetadataAddress`). `MapInboundClaims`
  is **false** on purpose — original OAuth2 claim names are preserved for inter-service
  use. Browser SignalR clients can't set headers on the WebSocket handshake, so the JS
  client sends the token as an `access_token` query param instead; `OnMessageReceived`
  promotes it to `context.Token` only for `/hubs/*` paths. `ClaimsValidationMiddleware`
  additionally enforces `RequiredClaims` on every authenticated request (403 if missing).

- **Pagination:** `PaginationResolver` fills in defaults from `PaginationRules`
  (`DefaultPageSize`) and validates via `PaginationParamsValidator` (FluentValidation).
  Resolution happens once, in `PaginationResolvingNotificationService` — the outermost
  decorator in the `INotificationService` chain — so the cache decorator and the base
  service both receive already-resolved skip/take and never resolve independently.

- **Localization:** error messages support `en` and `ru-by` via resx; culture flows
  through `UseLocalization`.

### Cross-cutting

- **Background jobs (Hangfire on Postgres):** used here purely as the message-redelivery
  scheduler for `ResilientConsumeFilter` (no periodic/recurring jobs). Dashboard only in
  Development.
- **Observability:** OpenTelemetry traces/metrics/logs → Aspire dashboard, Jaeger,
  Prometheus; Serilog → Console/File/Logstash/OTel. Health checks at `/health` cover DB,
  Kafka, Redis, Elasticsearch, Hangfire, Keycloak, telemetry backends, and the other three
  services' `/health` endpoints.

## Configuration

Settings bind from `appsettings.json` + env vars (double-underscore nesting, see
`docker-compose.yaml`) + .NET User Secrets for local dev (`UserSecretsId` in
`NotificationService.Api.csproj`). `KeycloakSettings`, `RedisSettings`, `KafkaSettings`,
`PaginationRules` are the strongly-typed options.

Run the full local stack with `docker-compose.yaml` (needs a populated `.env` — DB/Redis
passwords) alongside the shared `flow-overstack_common` network and other services'
compose files.
