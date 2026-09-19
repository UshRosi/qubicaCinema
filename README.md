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
| 1 | Catalog: movies, auditoriums, screenings | done |
| 2 | Booking: seat allocation, availability, cancellation, idempotency | planned |
| 3 | Event-driven integration over RabbitMQ, with the Catalog outbox | planned |
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
dotnet tool restore     # dotnet-ef, for adding migrations
dotnet build            # warnings are errors
dotnet test             # no Docker needed: the unit tests touch no infrastructure
dotnet run --project src/AppHost
```

The dashboard URL and its login token are printed in the console. The AppHost starts SQL Server with the
three databases, RabbitMQ with its management UI, and runs the migration service to completion before any
service starts.

### Trying the Catalog API

Once the `catalog` resource is healthy in the dashboard it answers on `http://localhost:5101`. The migration
service seeds three auditoriums, six films and a week of screenings, laid out relative to today so that they
are always in the future.

```bash
curl "http://localhost:5101/api/v1/screenings?pageSize=5"
curl "http://localhost:5101/api/v1/screenings?sort=CheapestFirst&from=2030-01-01T00:00:00Z"
curl -i "http://localhost:5101/api/v1/movies/{id}"                  # note the ETag header
curl -i -X PUT "http://localhost:5101/api/v1/movies/{id}" \
     -H 'Content-Type: application/json' -H 'If-Match: "<etag>"' \
     -d '{"title":"…","description":"…","durationMinutes":120,"genre":"Drama","ageRating":"Teen"}'
curl -i -X POST "http://localhost:5101/api/v1/screenings/{id}/cancellation" \
     -H 'Content-Type: application/json' -d '{"reason":"projector failure"}'
```

Migrations are added through the API project, so the tooling builds the real host and reads the same
configuration the service does; outside the AppHost the connection string comes from
`appsettings.Development.json`, overridable with user secrets or `ConnectionStrings__catalogdb`:

```bash
dotnet ef migrations add <Name> \
  --project src/Services/Catalog/Catalog.Infrastructure \
  --startup-project src/Services/Catalog/Catalog.Api
```

The endpoints are open for now; administrator-only writes arrive with authentication in chapter 5, and the
gateway in chapter 4 becomes the single entry point.

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
| `src/BuildingBlocks/BuildingBlocks.Application` | `ICommandHandler`, `IQueryHandler`, `PagedResult`, `Versioned`: the shapes every use case is written in |
| `src/BuildingBlocks/BuildingBlocks.Api` | Endpoint modules, the one ProblemDetails mapping, paging, validation and `If-Match` filters |
| `src/Services/Catalog/Catalog.Domain` | `Movie`, `Auditorium` with its `Seat`s, `Screening`; `Money`, `TimeSlot`, `SeatPosition` |
| `src/Services/Catalog/Catalog.Application` | One folder per use case (command or query, and its handler), and one port per repository and read model |
| `src/Services/Catalog/Catalog.Infrastructure` | EF Core mappings, migrations, repositories, read-side projections, the seed |
| `src/Services/Catalog/Catalog.Api` | `/api/v1/movies`, `/auditoriums`, `/screenings`, and the request validators |
| `tests/Catalog.UnitTests` | The domain rules and the use cases, with no container and no database |

Inside a service the dependencies point inwards: `Api → Application → Domain`, and `Infrastructure`
implements the ports that `Application` declares. The domain projects reference nothing but
`BuildingBlocks.Domain`.

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

**The domain decides, the API only translates.** Every rule — no overlapping screenings, cleaning time
between films, seats laid out by the auditorium itself — lives in an aggregate and is unit tested there. A
broken rule is a `DomainException` naming what went wrong, and one `IExceptionHandler` maps it to an RFC 9457
ProblemDetails with a stable `type` URI. That is why no endpoint and no handler contains a `try/catch`, and
why each endpoint is a few lines: bind, build the command, call the handler, return the result.

**No mediator.** Each use case is a handler class injected straight into its endpoint and registered by
name, one line each. Finding the code that runs is go-to-definition, and `ValidateOnBuild` — switched on in
every environment, not only Development — verifies the whole container at startup. Cross-cutting concerns
(validation, `If-Match`) are endpoint filters that each route declares, not a hidden pipeline.

**Status codes follow one rule.** Decidable from the payload alone is `422`; needs a database lookup is
`404` or `409`. An edit without `If-Match` is `428 Precondition Required`, a stale one is `412`. Missing
versions are refused rather than treated as "overwrite anything", because a lost update is silent.

**Cancellation is a state, not a deletion.** `POST /screenings/{id}/cancellation`, not `DELETE`: bookings in
another service refer to screening ids, so a cancelled screening stays readable. Cancelling twice succeeds
both times and changes nothing the second time, so a client can retry a request that timed out.

**Concurrency is checked twice.** The row version is compared with the caller's `If-Match` as soon as the
row is loaded, and handed to EF as the original value for the `UPDATE`. The comparison is needed because EF
sends no `UPDATE` at all when nothing changed, so a stale caller would otherwise be told it succeeded; the
original value covers the race between that read and the commit. The version is a shadow property: it is a
fact about the row, and the domain never sees it.

**Money is a decimal and a currency.** Two decimal places, ISO-4217 code, arithmetic refused across
currencies. That assumes every currency has two minor digits, which is not true of the yen; an integer
count of minor units is the robust alternative, and the one to prefer once amounts cross a service boundary.

**Offset paging, clamped.** Every list is paged, with the page size clamped server-side to 100 and a stable
tie-breaker on every sort so a row cannot fall between two pages. Offset paging drifts while rows are
inserted and slows on deep pages; a cursor would fix both, at the cost of "jump to page 7". Filters and
sorts are typed parameters, never a free-form string.

**Value objects are complex types.** `Money`, `TimeSlot` and `SeatPosition` are stored as columns of the
owning row. EF Core cannot declare an index that reaches into a complex type, so the two indexes that do —
seat positions unique per auditorium, and screenings by auditorium and start time — are written in the
initial migration, with the reason next to them.

**No SourceLink, symbols or package metadata.** Nothing here is published as a NuGet package, so that
machinery would be ceremony. `Directory.Build.props` carries only settings this solution actually uses.

**Analyzers at `latest-recommended`, not `latest-all`.** Warnings are errors, and `all` would turn hundreds
of stylistic opinions into build failures. The `.editorconfig` is short and every rule in it is deliberate,
including the ones switched off, each with its reason written next to it.

**One duplicated version.** `global.json` pins `Aspire.AppHost.Sdk` and `Directory.Packages.props` pins
`$(AspireVersion)` to the same number. MSBuild resolves SDKs before it evaluates properties, so the SDK
version cannot come from central package management; the two must be bumped together.

**Tests run on Microsoft.Testing.Platform.** xUnit v3 hosts the test platform itself, and the .NET 10 SDK
no longer runs it through VSTest, so `global.json` selects the runner and there is no
`Microsoft.NET.Test.Sdk` in the solution.

Decisions taken while building, and what they replaced, are recorded as the work goes on.
