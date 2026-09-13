# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this service is

`recipe-notification-service` is a .NET 10 Worker Service microservice in the Kjøkkenhylla platform. It consumes domain events off RabbitMQ (via MassTransit), renders Scriban HTML email templates, and delivers them over SMTP (MailKit). Failed deliveries are buffered transiently in MongoDB and exposed to an admin dashboard (`Core API`) via a request-response messaging protocol — this admin management surface is planned to move to a separate `recipe-system-api` microservice eventually (not started; see `Documentation/05-notification-management-and-planned-work.md`). Documentation and comments in this repo are written in Norwegian.

## Commands

```bash
# Build
dotnet build

# Run the worker (from Service/, needs RabbitMQ, MongoDB, and an SMTP endpoint — e.g. Mailpit — reachable per appsettings.json)
dotnet run --project Service

# Run all tests
dotnet test

# Run a single test class or method
dotnet test --filter "FullyQualifiedName~UserRegisteredProcessorTests"
dotnet test --filter "FullyQualifiedName~UserRegisteredProcessorTests.ProcessAsync_WhenValidEvent_ShouldRenderTemplateAndProcessWithRetry"
```

There is no lint/format command configured beyond the standard `dotnet build` warnings.

## Solution structure

- **Contracts** — shared MassTransit message contracts only (`Events/`, `Commands/`, `Queries/`), grouped by domain: `UserActions`, `SystemActions`, `AdminActions`, plus `NotificationManagement` for admin-facing commands/queries. No logic lives here.
- **Infrastructure** — the domain/business layer:
  - `Processors/` — one processor per event, grouped by the same domain folders as `Contracts`. A processor builds the Scriban template model, renders HTML, and hands off to `IPendingEmailService`. All processors implement a single shared `IEventProcessor<TEvent>` (`Processors/Interfaces/IEventProcessor.cs`) — there are no per-event `I{Name}Processor` interfaces.
  - `EmailDelivery/` — `EmailDeliveryService` (raw SMTP send via MailKit, throws `EmailDeliveryException`) and `PendingEmailService` (retry/hard-bounce/buffering orchestration, see below).
  - `TemplateService/` — `TemplateRenderService` (Scriban) and the `.html` templates under `Templates/{Domain}/`, mirroring the same domain grouping, plus a shared `Templates/_Layout.html` included via `{{ include "_Layout" }}` for header/footer/branding.
  - `State/` — `NotificationStateStore` (in-memory singleton flags) and `StateStoreInitializerHostedService` (startup check against MongoDB).
  - `Exceptions/` — `TemplateRenderException`, `EmailDeliveryException`.
- **Persistence** — MongoDB settings, `FailedNotification` entity, and `FailedNotificationRepository` for the `failed_notifications` transient buffer collection.
- **Service** — the Worker host: `Program.cs`, MassTransit `Consumers/` (mirrors the domain folders; one consumer per event/command, thin — just logs and delegates to the matching processor), and `Extensions/` for DI wiring (`InfrastructureExtensions`, `MassTransitExtensions`, `MailProcessorExtensions`, `SerilogExtensions`).
- **Tests** — xUnit + NSubstitute + Shouldly + `MassTransit.TestHarness`, mirroring `Infrastructure`/`Persistence`/`Service` structure.

Data flow for any event: `RabbitMQ → MassTransit Consumer → Processor (renders template) → PendingEmailService → EmailDeliveryService (SMTP)`, with failures flowing into MongoDB and back out to admin via `NotificationManagement` queries/commands.

## Core architectural rules (do not violate silently)

1. **Fail-fast on template errors.** Missing templates, Scriban syntax errors, or missing required model variables must throw `TemplateRenderException` and propagate unhandled — never swallow or retry these. This intentionally stops message ACK so RabbitMQ redelivers/holds.
2. **In-line retry, not requeue, for transient SMTP failures.** `PendingEmailService` retries transient `EmailDeliveryException`s up to `MaxAttempts = 5` in-process (with backoff), inside the same consumer scope. Don't push transient failures back onto the bus.
3. **Hard bounce detection short-circuits retries.** If the SMTP error text matches permanent-rejection patterns (`550`, `user unknown`, `recipient address rejected`, `mailbox unavailable`, `does not exist` — see `PendingEmailService.IsHardBounce`), stop after the first attempt and publish `InvalidEmailDetectedEvent` for `Auth API` to act on (e.g. lock/blacklist the account). Do not fall through to the 5-attempt retry loop for these.
4. **MongoDB (`failed_notifications`) is a transient dead-letter buffer only**, written after all 5 in-line attempts fail. A document is deleted **only** on successful (re-)delivery; a failed admin-triggered retry updates the existing document in place (`MarkRetryFailedAsync`) rather than deleting it or creating a duplicate — it is never auto-deleted just because a retry was attempted.
5. **Admin gets notified exactly once per failure episode.** `NotificationStateStore` (singleton, in-memory) tracks `HasPendingNotifications`/`HasNotifiedAdmin` so admin isn't spammed while multiple emails fail concurrently; the flags reset to `false` once the MongoDB buffer count reaches 0 again (see the `RetryFailedNotificationCommand`/`DeleteFailedNotificationCommand` consumers). `StateStoreInitializerHostedService` seeds `HasPendingNotifications` from MongoDB's document count at startup.
6. **All admin↔service communication is MassTransit request/response**, never a REST endpoint on this service — `GetFailedNotificationsQuery`, `RetryFailedNotificationCommand`, `DeleteFailedNotificationCommand`.
7. **All processors/consumers/templates are registered manually** in `MailProcessorExtensions` (processors, `Transient`) and `MassTransitExtensions` (consumers, `x.AddConsumer<...>()`) — there is no assembly scanning. New event handling must be wired into both.

## Adding a new email notification

Full checklist lives in `Documentation/02-events-and-messaging.md`; summary:

1. Add the event/command contract under `Contracts/{Events|Commands}/{Domain}/`.
2. Have the source service publish it via `IPublishEndpoint`.
3. Add a Scriban `.html` template under `Infrastructure/TemplateService/Templates/{Domain}/` — set `{{ title = "..." }}`, wrap the body in `{{ capture content }} ... {{ end }}`, then `{{ include "_Layout" }}` (see `Documentation/03-email-templates-and-design.md` for the design tokens and layout mechanics). No csproj change needed — the wildcard content glob picks it up.
4. Add `{EventName}Processor : IEventProcessor<{EventName}Event>` under `Infrastructure/Processors/{Domain}/`, rendering via `ITemplateRenderService.RenderTemplateAsync("{Domain}/{TemplateName}", model)` and sending via `IPendingEmailService.ProcessEmailWithRetryAsync(..., nameof(EventName), ...)`.
5. Add a thin `{EventName}Consumer : IConsumer<TEvent>` under `Service/Consumers/{Domain}/` that takes `IEventProcessor<TEvent> processor` and just logs + calls it.
6. Register the processor (`Transient`, against `IEventProcessor<TEvent>`) in `MailProcessorExtensions.cs` and the consumer in `MassTransitExtensions.cs`, under the matching domain section.
7. Add tests: processor unit test (mock `ITemplateRenderService`/`IPendingEmailService`, assert exact template path + model + subject/body/event-type), template integration test (confirm it renders from disk without error), and a consumer test via `MassTransit.TestHarness`.

## Testing conventions

See `Documentation/06-test-strategy.md` for the full rationale. Key points:

- Processors are tested in isolation with `NSubstitute` mocks of `ITemplateRenderService` and `IPendingEmailService`; assertions check the **exact** template path string and that `ProcessEmailWithRetryAsync` receives the right subject/body/`nameof(Event)`.
- `PendingEmailService` tests cover: first-attempt success, transient-failure-then-success, all-5-attempts-fail (assert MongoDB write + deduped admin email), hard bounce (assert immediate stop + `InvalidEmailDetectedEvent` publish), and the admin-retry path (`existingNotificationId` set: delete-on-success, update-in-place-never-delete-or-duplicate on renewed failure/hard bounce).
- `TemplateRenderService` tests are integration tests that actually compile/render the `.html` files from disk.
- Consumers use `MassTransit.TestHarness` rather than a real RabbitMQ broker.
- Assertions use Shouldly (`result.ShouldBeTrue()`), not xUnit's `Assert`.

## Configuration

Settings come from `Service/appsettings.json` (`AppSettings:FrontendUrl`, `MongoDbSettings`, `RabbitMQ`, `SmtpSettings`, `Serilog`) with environment-variable overrides in this repo's own `docker-compose.yml`. `AppSettings:FrontendUrl` is validated on startup (`ValidateOnStart`) and the process refuses to start without it. Local dev expects RabbitMQ, MongoDB, and Mailpit running via the separate `recipe-infrastructure` project's docker-compose stack, which also creates the shared `recipe-net` network this service's own container joins. The `Dockerfile` uses `mcr.microsoft.com/dotnet/aspnet:10.0` (not `dotnet/runtime`) as its runtime base — `Serilog.AspNetCore` requires the ASP.NET Core shared framework at runtime even though this is a Worker Service with no HTTP endpoints (verified: the app fails to launch on `dotnet/runtime`).
