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
| 2 | Booking: seat allocation, availability, cancellation, idempotency | done |
| 3 | Event-driven integration over RabbitMQ: Catalog outbox, Booking inbox | done |
| 4 | YARP gateway: routing, rate limiting, one error shape | done |
| 5 | Identity and JWT: register, log in, roles, one signing key, protected routes | done |
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

### Trying the API

Everything is reached through the gateway. Aspire assigns its port on every run, so read the `gateway`
endpoint from the dashboard and put it in a variable. The migration service seeds three auditoriums, six
films and a week of screenings, laid out relative to today so that they are always in the future, and an
administrator account.

Reading the programme and the seat map needs no token. Writing to the catalogue needs the administrator's,
and booking needs a customer's. The seeded administrator is `admin@qubicacinema.local` with the password
`Admin!Cinema1` (local development values, set in `src/AppHost/appsettings.json`).

```bash
GW=http://localhost:<the gateway port from the dashboard>

# The administrator's token, for the writes below.
ADMIN=$(curl -s -X POST "$GW/api/v1/auth/login" -H 'Content-Type: application/json' \
     -d '{"email":"admin@qubicacinema.local","password":"Admin!Cinema1"}' | jq -r .accessToken)

# A customer, for bookings. Registering creates a Customer; there is no way to register as an administrator.
curl -i -X POST "$GW/api/v1/auth/register" -H 'Content-Type: application/json' \
     -d '{"email":"ada@example.com","password":"Correct-horse-1","firstName":"Ada","lastName":"Lovelace"}'
CUSTOMER=$(curl -s -X POST "$GW/api/v1/auth/login" -H 'Content-Type: application/json' \
     -d '{"email":"ada@example.com","password":"Correct-horse-1"}' | jq -r .accessToken)

curl "$GW/api/v1/screenings?pageSize=5"
curl "$GW/api/v1/screenings?sort=CheapestFirst&from=2030-01-01T00:00:00Z"
curl -i "$GW/api/v1/movies/{id}"                                    # note the ETag header
curl -i -X PUT "$GW/api/v1/movies/{id}" -H "Authorization: Bearer $ADMIN" \
     -H 'Content-Type: application/json' -H 'If-Match: "<etag>"' \
     -d '{"title":"…","description":"…","durationMinutes":120,"genre":"Drama","ageRating":"Teen"}'
curl -i -X POST "$GW/api/v1/screenings/{id}/cancellation" -H "Authorization: Bearer $ADMIN" \
     -H 'Content-Type: application/json' -d '{"reason":"projector failure"}'

# The seat map is Booking's, the rest of /screenings is Catalog's; the client cannot tell.
curl -i "$GW/api/v1/screenings/{id}/seats"                          # Cache-Control: no-store
curl -i -X POST "$GW/api/v1/bookings" -H "Authorization: Bearer $CUSTOMER" \
     -H 'Content-Type: application/json' -H "Idempotency-Key: $(uuidgen)" \
     -d '{"items":[{"screeningId":"{id}","selection":{"mode":"quantity","quantity":2}}]}'
```

Migrations are added through the API project, so the tooling builds the real host and reads the same
configuration the service does; outside the AppHost the connection string comes from
`appsettings.Development.json`, overridable with user secrets or `ConnectionStrings__catalogdb`:

```bash
dotnet ef migrations add <Name> \
  --project src/Services/Catalog/Catalog.Infrastructure \
  --startup-project src/Services/Catalog/Catalog.Api
```

The services also listen on ports of their own, which is what their launch profiles are for when one is started
by hand; under the AppHost, treat the gateway as the only door.

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
| `src/Gateway` | The YARP reverse proxy: the routing table is `appsettings.json`, and the code is the rate limiter and the mapping of gateway errors to ProblemDetails. No Aspire package |
| `src/ServiceDefaults` | OpenTelemetry and health-check plumbing shared by every process. No Aspire package |
| `src/MigrationService` | Applies every schema once and exits; a non-zero exit code stops the services from starting |
| `src/BuildingBlocks/BuildingBlocks.Domain` | `Entity`, `AggregateRoot`, `IDomainEvent`, `IUnitOfWork`, `DomainException`. No dependencies |
| `src/BuildingBlocks/BuildingBlocks.Persistence` | `IDatabaseInitializer`, the idempotency store, and the transactional outbox and inbox, generic over a service's `DbContext` |
| `src/BuildingBlocks/BuildingBlocks.Contracts` | The integration events (`ScreeningScheduled`, `ScreeningRescheduled`, `ScreeningCancelled`): flat records with a stable wire name. No dependencies |
| `src/BuildingBlocks/BuildingBlocks.EventBus` | What a service knows about messaging and nothing about the broker: `IEventBus`, `IIntegrationHandler`, `IInbox` |
| `src/BuildingBlocks/BuildingBlocks.EventBus.RabbitMQ` | The publisher, the consumer, the topology and the health check, written on the official `RabbitMQ.Client` |
| `src/BuildingBlocks/BuildingBlocks.Application` | `ICommandHandler`, `IQueryHandler`, `PagedResult`, `Versioned`: the shapes every use case is written in |
| `src/BuildingBlocks/BuildingBlocks.Authentication` | `JwtOptions` and its validator, the bearer configuration, the `Admin` / `Customer` / `authenticated` policies and the `ICurrentUser` that reads the validated token. Referenced by the gateway, Catalog, Booking and Identity |
| `src/BuildingBlocks/BuildingBlocks.Api` | Endpoint modules, the one ProblemDetails mapping, paging, validation and `If-Match` filters |
| `src/Services/Catalog/Catalog.Domain` | `Movie`, `Auditorium` with its `Seat`s, `Screening`; `Money`, `TimeSlot`, `SeatPosition` |
| `src/Services/Catalog/Catalog.Application` | One folder per use case (command or query, and its handler), and one port per repository and read model |
| `src/Services/Catalog/Catalog.Infrastructure` | EF Core mappings, migrations, repositories, read-side projections, the seed |
| `src/Services/Catalog/Catalog.Api` | `/api/v1/movies`, `/auditoriums`, `/screenings`, and the request validators |
| `src/Services/Bookings/Bookings.Application` | Booking's use cases, and one handler per Catalog event that keeps its screenings and seats current |
| `src/Services/Identity/Identity.Persistence` | The user and role types, the `DbContext`, the seeded roles and administrator, and the registration of ASP.NET Core Identity's stores |
| `src/Services/Identity/Identity.Api` | `POST /api/v1/auth/register` and `/login`, and the token service that signs the JWT |
| `tests/Identity.UnitTests` | The token service, the register and login handlers, and a token signed by Identity accepted by the real bearer validation, refused for the wrong role, key, audience or age |
| `tests/Catalog.UnitTests` | The domain rules and the use cases, with no container and no database |
| `tests/Gateway.IntegrationTests` | The real gateway hosted in memory with both services replaced by a recording stub: routing, headers, error shape and limits, with no container |
| `tests/Bookings.UnitTests`, `tests/BuildingBlocks.UnitTests` | Booking's domain and handlers; the shared kernel, the event contracts and the outbox |

Inside a service the dependencies point inwards: `Api → Application → Domain`, and `Infrastructure`
implements the ports that `Application` declares. The domain projects reference nothing but
`BuildingBlocks.Domain`.

## How the services talk

```
Catalog  ── SaveChanges ──►  Screenings + OutboxMessages   (one transaction)
                                       │
              outbox publisher  ◄──────┘   claims a batch, publishes in order, stops at the first failure
                    │  publisher confirms · mandatory
                    ▼
        RabbitMQ  topic exchange "qubica.events"  ── routing key = event name
                    │
                    ▼  quorum queue "booking"
        Booking consumer:  inbox check → handler → inbox row + changes in ONE SaveChanges → ack
                    │ transient failure                      │ permanent failure, or retries used up
                    ▼                                        ▼
        "booking.retry.1|2|3"  (TTL 5 s · 30 s · 2 min,      "booking.dead-letter"
         then back to "booking")
```

Scheduling a screening in Catalog makes its seat map appear in Booking within a second or so; cancelling it
releases every seat booked for it. Stopping RabbitMQ leaves Catalog writable (the rows wait in the outbox,
with their attempt count and last error) and Booking readable; both recover when the broker returns.

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

**The gateway's health is its own, and a dead service degrades one route.** `/health` on the gateway answers
for the gateway. If a stopped Catalog made it unhealthy, a load balancer would pull the gateway out of
rotation and take login and booking reads down with it. Downstream health is YARP's own active check, per
cluster: after three failed probes a service has no healthy destination and only its routes answer `503`,
with an `upstream-unavailable` problem. YARP's default, `HealthyOrPanic`, would forward to a dead
destination anyway when it is the only one, so the clusters pin `HealthyAndUnknown`. Without that the health
check would be decorative. Nothing proxies `/health` or `/alive`, because every route lives under `/api/v1`
and there is no catch-all.

**A path split across two services is resolved by specificity, not by `Order`.** `/api/v1/screenings/{id}/seats`
belongs to Booking and the rest of `/screenings` to Catalog. ASP.NET Core already prefers the literal
`seats` segment over a catch-all, so no route carries an `Order`. Setting one would be worse than redundant:
`Order` is compared before specificity across every route, so an `Order` added later for an unrelated
reason could let a catch-all swallow the seat map. A test pins the split instead of forcing it.

**The gateway rewrites nothing.** There are no transforms: the path, the request headers (`If-Match`,
`Idempotency-Key`, `Authorization`) and the response headers (`ETag`, `Cache-Control`, `Location`) cross
untouched. That is why the absolute `Location: /api/v1/bookings/{id}` a service returns is still correct
at the gateway. A test asserts it, so adding a transform that drops one fails the build.

**One error shape, from whoever answers.** A status the gateway produces itself (`404` for an unrouted path,
`429`, `502`, `503`, `504`) is turned into the same RFC 9457 ProblemDetails, with `type` URIs from the same
registry the services use. A service's own error is passed through untouched. The three upstream failures
have three types because the advice differs: `502`, the service answered badly, so do not retry blindly;
`503`, nothing healthy is listening, so retry later; `504`, it was too slow and may have committed anyway,
which for `POST /bookings` is exactly what the `Idempotency-Key` is for.

**Rate limiting is per client, fixed window, and stricter where it costs something.** A global limiter
covers every route, so one added later is protected before anyone remembers to name a policy on it; creating
a booking has a smaller budget of its own, because a hot loop there consumes seats. A fixed window because
it is the only algorithm that hands back an exact `Retry-After`; the price is a burst of up to twice the
limit across a window edge. The client is the token's subject when there is a valid one, so an account
cannot multiply its budget by changing address, and the remote address otherwise. Startup refuses a booking
budget that is not smaller than the global one, since that policy would never refuse anything.

**No blanket retries at the gateway.** An automatic retry of `POST /bookings` can double-book unless the
idempotency key is honoured, so retries belong to the client, which knows whether it may repeat a request.
The gateway's resilience is a timeout per route, an idle timeout per cluster and the health checks above.

**The AppHost hands the gateway two strings, and nothing else.** Each service's address goes in as
`ReverseProxy__Clusters__<id>__Destinations__primary__Address`, the key the gateway's own `appsettings.json`
declares, rather than through `WithReference`, which would inject service-discovery keys nothing reads.
Running the gateway without Aspire means setting those two variables. The gateway also does not wait for the
services: that independence is what keeps it up while one of them is down.

**Liveness never checks a dependency.** `/alive` answers for the process only; `/health` is where
dependencies belong. A liveness probe that pinged SQL Server would turn a thirty-second database blip into
a restart of every healthy instance. Both endpoints are mapped only in Development, or where
`HealthChecks:Expose` is set, and only Development gets the detailed body that names each dependency. The
AppHost sets `HealthChecks__Expose` on Catalog and Booking, because the gateway probes their `/health`: a
probe that got a `404` would mark a perfectly healthy service dead.

**Fixed local credentials.** The SA password, the JWT signing key and the seeded administrator's password are
fixed development values declared in the AppHost. A generated password lives in user secrets, so clearing those while a data volume survives
leaves SQL Server unable to start: the password no longer matches the persisted master database. These
values never leave a developer machine.

**Identity is two projects, not four.** ASP.NET Core Identity is the model there, so a Domain and an
Application layer would only wrap `UserManager` in interfaces of our own. It is split in two rather than one
because the migration service has to create the schema and seed the administrator without referencing a web
application: `Identity.Persistence` is what it references, and `Identity.Api` is the HTTP face.

**One signing key, validated four times.** Identity signs with HMAC-SHA256 and the gateway, Catalog and Booking
each validate with the same key, handed to all four by the AppHost as `Jwt__Issuer`, `Jwt__Audience` and
`Jwt__SigningKey`. Each process checks the options when it starts, so a missing or short key stops it with a
sentence naming the fix instead of surfacing as a 401 on every request, and the committed development key is
refused outside Development. A symmetric key is the right size for this exercise. The production evolution is
RS256 with a JWKS endpoint: the validators would then hold only the public key, and the secret would never
leave the issuer.

**The gateway rejects early; the services decide.** The routes carry a policy, so an unauthenticated write
never reaches a service, but the gateway is not a trust boundary: Catalog and Booking validate the token again,
because a service reachable any other way must not assume somebody checked. Which booking a caller may see is
decided in the use case, which has the loaded booking, and someone else's booking is a `404`, not a `403`, so
ids cannot be probed. An administrator can read and cancel any booking but cannot book: booking needs the
`Customer` role, everything else only a valid token.

**Login never says which half was wrong.** An unknown email and a wrong password are the same `401` with the
same message. The time taken still differs, because an unknown email skips the password hash; the gateway's
rate limit is what bounds how much that can be used to probe.

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

**The outbox, not a publish call.** A use case never touches the event bus. Catalog stages an outbox row in
`SaveChanges`, in the same transaction as the change it announces, and a background service delivers it.
Publishing from a handler would be a second write outside that transaction: a crash between the two would
leave a screening that exists and was never announced. The row also stores the W3C trace context it was
written in, so the trace from the API call runs on through the broker into Booking.

**Events are a wire contract with a stable name.** `catalog.screening-scheduled.v1` is a literal, never a CLR
type name: renaming a class must not orphan a stored row or a message in flight. Fields are only ever added,
and optionally; readers ignore what they do not know. Changing a meaning is a `…v2` type, with the reader
deployed before the writer. `ScreeningScheduled` carries the auditorium's whole seat map, so Booking needs no
earlier state and a replay is trivially correct, at the price of repeating the seat list for every
screening in a room. The normalised alternative — an auditorium event plus a lean screening event — would
force Booking to cope with a screening arriving before its auditorium.

**At-least-once delivery, effectively-once processing.** The consumer records each event in an inbox table in
the same transaction as the handler's changes and acknowledges only after the commit. A crash between the
commit and the ack redelivers the message, and the inbox turns it into a no-op.

**Retry with backoff, and only for what time can cure.** A failed handler is not requeued at once: an
immediate retry would use every attempt in milliseconds, well inside a database restart. A small classifier
asks whether waiting changes the answer. A timeout, a lost race, or a screening whose announcement has not
arrived yet is transient: the message is parked in a retry queue whose time-to-live grows with each attempt
(5 s, 30 s, 2 min) and dead-lettered back to the consumer's own queue when it expires. A payload that cannot
be read, or data the model refuses, will fail identically forever and goes straight to the dead-letter queue.
There is one retry queue per delay rather than one delay per message, because only the message at the head
of a queue can expire. The retry queues dead-letter to the consumer's queue directly, not through the events
exchange, which would hand the message to every other service as well. The message is confirmed in the retry
queue before the original is acknowledged, so a failure in between duplicates it instead of losing it.

**Ordered, single publisher.** The outbox publishes in order and stops at the first failure, so a cancellation
can never overtake the announcement of its screening; one stuck message therefore holds back the ones behind
it. That ordering holds for one publisher instance, and the consumer is likewise sequential. Running several
of either would need a per-screening sequence number.

**Cancelling a screening cancels its bookings, and Booking publishes nothing.** A confirmed booking for a
screening that will never happen is a state nobody can explain, so the consumer releases the seats. It does
not announce that in turn: doing so from inside a handler would reintroduce the dual write the outbox exists
to avoid. If a notification service ever needs it, Booking gets an outbox of its own.

**RabbitMQ being down is a degradation, not an outage.** The RabbitMQ health check reports `Degraded`, which
still answers `200`: reads in Booking do not need the broker, and Catalog's writes are held by the outbox.

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
