# QubicaCinema

A cinema seat-booking backend: REST APIs to browse screenings, check seat availability and book seats —
either by picking them on the seat map or by asking for a number of seats and letting the system allocate
them. Built as a set of .NET microservices (Identity, Catalog, Booking) behind a YARP gateway, orchestrated
locally with .NET Aspire, integrated through RabbitMQ and persisted with EF Core on SQL Server.

## Status

Work in progress, built chapter by chapter. The plan lives in [docs/plan/](docs/plan/) and the decisions
taken along the way in [docs/plan/DECISIONS.md](docs/plan/DECISIONS.md).

| Chapter | Scope | State |
|---|---|---|
| 0 | Bootstrap: build configuration, AppHost, ServiceDefaults, domain building blocks | in progress |
| 1–9 | Catalog, Booking, messaging, gateway, identity, docs, tests, CI, polish | planned |

## Prerequisites

- [.NET SDK 10.0.103](https://dotnet.microsoft.com/download) or newer (pinned in `global.json`)
- Docker, running — the AppHost starts SQL Server and RabbitMQ as containers.
  SQL Server needs roughly 2 GB of memory and 20–40 seconds to become healthy on a first run.

## How to run

```bash
dotnet tool restore
dotnet build
dotnet run --project src/AppHost
```

The Aspire dashboard URL and its login token are printed in the console.

## Architecture

To be documented at the end of chapter 0.

## Trade-offs

To be documented at the end of chapter 0.
