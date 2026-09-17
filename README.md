# QubicaCinema

A cinema seat-booking backend: browse screenings, check seat availability and book seats — either by
picking them on the seat map or by asking for a number of seats and letting the system allocate them.
Built as a set of .NET microservices behind a gateway, integrated through RabbitMQ and persisted with
EF Core on SQL Server.

## Status

The solution is built chapter by chapter, and each chapter ends with commits that build on their own.

| Chapter | Scope | State |
|---|---|---|
| 0 | Bootstrap: build configuration, ServiceDefaults, AppHost, migration service, domain building blocks | done |
| 1 | Catalog: movies, auditoriums, screenings, outbox | planned |
| 2 | Booking: seat allocation, availability, cancellation, idempotency | planned |
| 3 | Event-driven integration over RabbitMQ | planned |
| 4 | YARP gateway | planned |
| 5 | Identity and JWT | planned |
| 6–9 | API documentation, tests, CI, final polish | planned |

## Prerequisites

- [.NET SDK 10.0.103](https://dotnet.microsoft.com/download) or newer. The version is pinned in
  `global.json`, so a machine with several SDKs installed still builds this solution with the same one.
- Docker, running. The first start pulls `mcr.microsoft.com/mssql/server:2022-latest` (about 1.5 GB) and
  `rabbitmq:4.3-management`. SQL Server needs roughly 2 GB of memory and 20–40 seconds to become healthy.

No other tool is required: the Aspire dashboard and orchestrator are restored from NuGet, so the `aspire`
CLI does not have to be installed.

## How to run

```bash
dotnet tool restore     # dotnet-ef, used from chapter 1 onwards
dotnet build
dotnet run --project src/AppHost
```

The dashboard URL and its login token are printed in the console. The AppHost starts SQL Server with the
three databases, RabbitMQ with its management UI, and runs the migration service to completion before any
service starts.

Local development behaviour is configurable through the `Cinema` section — for example
`Cinema__UseVolumes=true` to keep the SQL Server data between runs, or `Cinema__PersistentContainers=true`
to leave the containers up after the AppHost exits. Both default to false so that every run starts from the
same known state.

## Architecture

```
                 client (API docs · .http files)
                              │  Bearer JWT
                        ┌─────▼─────┐
                        │  Gateway  │  YARP · JWT validation · rate limiting
                        └──┬───┬───┬┘
             ┌─────────────┘   │   └──────────────┐
       ┌─────▼─────┐     ┌─────▼─────┐      ┌─────▼─────┐
       │ Identity  │     │  Catalog  │      │  Booking  │
       └─────┬─────┘     └─────┬─────┘      └─────┬─────┘
         identitydb        catalogdb ──events──►  bookingdb
                                  RabbitMQ
   ┌──────────────────┐
   │ MigrationService │  runs to completion first; every service waits for it
   └──────────────────┘
```

Each service owns its database, and ids from another service are logical references with no foreign key.
Services never call each other over HTTP: everything that crosses a boundary is an integration event.

### What exists today

| Project | Role |
|---|---|
| `src/AppHost` | The only project that references Aspire. Declares SQL Server, RabbitMQ and the migration service |
| `src/ServiceDefaults` | OpenTelemetry and health-check plumbing shared by every process. No Aspire package |
| `src/MigrationService` | Applies every schema once and exits; a non-zero exit code stops the services from starting |
| `src/BuildingBlocks/BuildingBlocks.Domain` | `Entity`, `AggregateRoot`, `IDomainEvent`, `IUnitOfWork`, `DomainException`. No dependencies |
| `src/BuildingBlocks/BuildingBlocks.Persistence` | `IDatabaseInitializer`, the seam between a service's schema and the migration service |

## Trade-offs

**Aspire stays in the AppHost.** No `Aspire.*` client integration is referenced by a service, and neither
service discovery nor the standard resilience handler is used. Client integrations would hide the retry
policy, the health check and the instrumentation behind one call, and those are exactly the parts worth
reading here. The services are configured through ordinary keys — `ConnectionStrings__catalogdb`,
`Jwt__SigningKey` — so the same build runs anywhere those environment variables are set. Service discovery
would be dead weight because no service calls another over HTTP, and `IHttpClientFactory` resilience would
never run: the gateway builds its own message invoker per cluster. Blanket retries over `POST /bookings`
could also double-book, so resilience goes where it is real — gateway timeouts and an outbox publisher that
is idempotent by construction.

**Liveness never checks a dependency.** `/alive` answers for the process only; `/health` is where
dependencies belong. A liveness probe that pinged SQL Server would turn a thirty-second database blip into
a restart of every healthy instance. Both endpoints are mapped only in Development, or where
`HealthChecks:Expose` is set, and only Development gets the detailed body that names each dependency.

**Fixed local credentials.** The SA password and the JWT signing key are fixed development values declared
in the AppHost. A generated password lives in user secrets, so clearing those while a data volume survives
leaves SQL Server unable to start: the password no longer matches the persisted master database. These
values never leave a developer machine.

**No SourceLink, symbols or package metadata.** Nothing here is published as a NuGet package, so that
machinery would be ceremony. `Directory.Build.props` carries only settings this solution actually uses.

**Analyzers at `latest-recommended`, not `latest-all`.** Warnings are errors, and `all` would turn hundreds
of stylistic opinions into build failures. The `.editorconfig` is short and every rule in it is deliberate,
including the ones switched off, each with its reason written next to it.

**One duplicated version.** `global.json` pins `Aspire.AppHost.Sdk` and `Directory.Packages.props` pins
`$(AspireVersion)` to the same number. MSBuild resolves SDKs before it evaluates properties, so the SDK
version cannot come from central package management; the two must be bumped together.

Decisions taken while building, and what they replaced, are recorded as the work goes on.
