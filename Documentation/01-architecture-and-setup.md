# 🏛️ Arkitektur og oppsett

## Formål

**Recipe Notification Service** er en spesialisert mikrotjeneste i Kjøkkenhylla-plattformen. Tjenesten
prosesserer, rendrer og leverer e-postnotifikasjoner asynkront basert på hendelser fra meldingsbussen
(RabbitMQ), og håndterer feilsituasjoner og administrasjon av uleverte meldinger via en transient
MongoDB-buffer.

Tjenesten kjører som en **.NET 10 Worker Service** (ingen HTTP-endepunkter) — all inn- og utgående
kommunikasjon skjer over MassTransit/RabbitMQ.

## Teknologistack

| Formål | Teknologi |
| --- | --- |
| Meldingsbuss | MassTransit over RabbitMQ |
| E-postutsending | MailKit (SMTP) |
| Malmotor | Scriban |
| Transient feilbuffer | MongoDB (`MongoDB.Driver`) |
| Logging | Serilog (Console + Seq-sink) |
| Testing | xUnit, NSubstitute, Shouldly, MassTransit.TestHarness |

## Prosjektstruktur

```text
recipe-notification-service/
├── Contracts/                          # Delte MassTransit-kontrakter (Events, Commands, Queries) - ingen logikk
│   ├── Events/{UserActions,SystemActions,AdminActions}/
│   ├── Commands/NotificationManagement/
│   └── Queries/NotificationManagement/
├── Persistence/                        # MongoDB-tilgang
│   ├── Configurations/MongoDbSettings.cs
│   ├── Entities/FailedNotification.cs
│   └── Repositories/FailedNotificationRepository.cs
├── Infrastructure/                     # Domenelogikk
│   ├── Processors/{Domain}/            # Én prosessor per event - bygger malmodell og sender via PendingEmailService
│   ├── Processors/Interfaces/          # Ett delt IEventProcessor<TEvent>-grensesnitt
│   ├── EmailDelivery/                  # EmailDeliveryService (SMTP) + PendingEmailService (retry/buffer)
│   ├── TemplateService/                # TemplateRenderService, TemplateFileSystemLoader + Templates/
│   ├── State/                          # NotificationStateStore + StateStoreInitializerHostedService
│   ├── Options/                        # AppSettings + AppSettingsExtensions
│   └── Extensions/                     # DateTimeExtensions
├── Service/                            # Worker-vert
│   ├── Consumers/{Domain}/             # Tynne MassTransit-consumers - logger og kaller riktig prosessor
│   ├── Extensions/                     # DI-oppsett (Infrastructure/MassTransit/Processors/Serilog)
│   ├── Program.cs
│   └── appsettings*.json
├── Tests/                              # Speiler Infrastructure/Persistence/Service
└── Documentation/                      # Denne mappen
```

## Dataflyt

Alle hendelser følger samme kjede, uavhengig av domene:

```text
RabbitMQ-event
      │
      ▼
MassTransit Consumer (Service/Consumers/{Domain})   - logger + delegerer
      │
      ▼
IEventProcessor<TEvent> (Infrastructure/Processors/{Domain})
      │  bygger Scriban-modell
      ▼
ITemplateRenderService.RenderTemplateAsync(...)      - se 03-email-templates-and-design.md
      │  ferdig HTML
      ▼
IPendingEmailService.ProcessEmailWithRetryAsync(...) - se 04-error-handling-and-resilience.md
      │
      ▼
IEmailDeliveryService (MailKit/SMTP)
```

Se [02-events-and-messaging.md](02-events-and-messaging.md) for kontraktene og prosessor/consumer-mønsteret,
og [04-error-handling-and-resilience.md](04-error-handling-and-resilience.md) for hva som skjer når
utsendingen feiler.

## Konfigurasjon

Innstillinger kommer fra `Service/appsettings.json` / `appsettings.Development.json`, med
miljøvariabel-overstyring i Docker Compose:

| Seksjon | Nøkler | Merknad |
| --- | --- | --- |
| `AppSettings` | `FrontendUrl` | Validert med `ValidateOnStart()` - prosessen nekter å starte uten denne. |
| `MongoDbSettings` | `ConnectionString`, `DatabaseName`, `FailedNotificationsCollection` | Transient buffer for uleverte e-poster. |
| `RabbitMQ` | `Host`, `Port`, `VirtualHost`, `Username`, `Password` | |
| `SmtpSettings` | `Host`, `Port`, `EnableSsl`, `DefaultSenderEmail`, `DefaultSenderName`, `AdminNotificationEmail` | `AdminNotificationEmail` brukes både av kontaktskjema-varsling og av admin-varselet ved feilet e-postutsending. |
| `Serilog` | `MinimumLevel`, `WriteTo` (Console + Seq) | |

## Kjøring lokalt

Tjenesten forventer at følgende kjører (normalt via plattformens felles `docker-compose`-oppsett):

* **RabbitMQ** - meldingsbuss
* **MongoDB** - transient feilbuffer
* **Mailpit** (eller tilsvarende) - lokal SMTP-mottaker på `localhost:1025`
* **Seq** - loggvisning

```bash
dotnet run --project Service
```

## Bygge og teste

```bash
dotnet build
dotnet test

# Kjør én testklasse/metode
dotnet test --filter "FullyQualifiedName~UserRegisteredProcessorTests"
```
