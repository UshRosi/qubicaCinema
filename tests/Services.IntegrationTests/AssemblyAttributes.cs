using Xunit.Sdk;
using Xunit.v3;
using QubicaCinema.Services.IntegrationTests.Fixtures;

// One SQL Server container, one RabbitMQ container and three hosts for the whole assembly: see CinemaFixture.
[assembly: AssemblyFixture<CinemaFixture>]

// Every test in this assembly shares those containers and hosts, so they must run one at a time. A missing
// xunit.runner.json would fail open into parallel tests racing on one database; this attribute cannot.
[assembly: Parallelization(Mode = ParallelMode.None)]
