using Microsoft.Extensions.Time.Testing;
using QubicaCinema.BuildingBlocks.Contracts.Catalog;
using QubicaCinema.BuildingBlocks.EventBus.RabbitMQ;
using QubicaCinema.Bookings.Infrastructure;
using QubicaCinema.Catalog.Infrastructure;
using QubicaCinema.Identity.Persistence;

namespace QubicaCinema.Services.IntegrationTests.Fixtures;

/// <summary>
/// The one SQL Server container, the one RabbitMQ container and the three hosts, shared by every test in
/// this assembly.
/// </summary>
/// <remarks>
/// Registered with <c>[assembly: AssemblyFixture&lt;CinemaFixture&gt;]</c>, so it is built once, before any
/// test runs, and torn down once, after the last one finishes. A test class asks for it the same way an
/// <c>ICollectionFixture</c> consumer would: a constructor parameter of this exact type.
/// <para>
/// Configuration is set as <b>process environment variables</b>, not through each factory's
/// <c>ConfigureAppConfiguration</c>. Every <c>Add*Infrastructure(connectionString)</c> and
/// <c>AddCatalogOutboxPublisher(runPublisher: builder.Configuration.GetValue(...))</c> reads
/// <c>builder.Configuration</c> <em>eagerly</em>, as a plain value, on the line in <c>Program.cs</c> that
/// calls it — and that line runs before a <c>WebApplicationFactory</c>'s override is merged in, which only
/// happens around <c>builder.Build()</c>. An in-memory collection added through
/// <c>ConfigureAppConfiguration</c> therefore arrives too late for those two calls specifically (it works
/// fine for the JWT options and the health/OpenAPI exposure flags, which are bound from a live
/// <c>IConfigurationSection</c> or read after <c>Build()</c>). Environment variables have no such problem:
/// <c>WebApplication.CreateBuilder</c> reads them as one of its default sources, and this process sets them
/// before it ever calls <c>CreateBuilder</c> — the same mechanism the AppHost uses in every other deployment,
/// just set here instead of by an orchestrator.
/// </para>
/// </remarks>
public sealed class CinemaFixture : IAsyncLifetime
{
    private static readonly TimeSpan BindingsTimeout = TimeSpan.FromSeconds(30);

    private readonly SqlServerFixture _sql = new();
    private readonly RabbitMqFixture _rabbit = new();

    /// <summary>
    /// The clock Catalog and Bookings share, frozen so every test computes its own <c>startsAt</c> from it
    /// instead of racing the real time a screening must be in the future of. Identity keeps the real clock —
    /// see <see cref="IdentityApp"/>.
    /// </summary>
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 11, 2, 9, 0, 0, TimeSpan.Zero));

    /// <summary>The real Catalog service.</summary>
    internal CatalogApp Catalog { get; private set; } = null!;

    /// <summary>The real Booking service.</summary>
    internal BookingsApp Bookings { get; private set; } = null!;

    /// <summary>The real Identity service.</summary>
    internal IdentityApp Identity { get; private set; } = null!;

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await Task.WhenAll(
            _sql.StartAsync(cancellationToken),
            _rabbit.StartAsync(cancellationToken));

        SetEnvironment();

        Catalog = new CatalogApp(Clock);
        Bookings = new BookingsApp(Clock);
        Identity = new IdentityApp();

        // Sequential: all three touch the same server's master database while creating theirs, and there is
        // no time pressure here worth racing for.
        await Catalog.MigrateAsync(cancellationToken);
        await Bookings.MigrateAsync(cancellationToken);
        await Identity.MigrateAsync(cancellationToken);

        // Bookings.MigrateAsync already touched Services, which started the host and, with it,
        // RabbitMqConsumerService — the queue and its bindings are declared on a background task from here.
        await _rabbit.WaitForBookingBindingsAsync(
            [CatalogEventNames.ScreeningScheduled, CatalogEventNames.ScreeningRescheduled, CatalogEventNames.ScreeningCancelled],
            BindingsTimeout,
            cancellationToken);
    }

    /// <summary>
    /// Waits until Booking's queue has no message waiting or unacknowledged — the fact that a message just
    /// published into it has been delivered, handled and acked.
    /// </summary>
    internal Task WaitForBookingQueueIdleAsync(CancellationToken cancellationToken) =>
        _rabbit.WaitForQueueIdleAsync("booking", TimeSpan.FromSeconds(10), cancellationToken);

    /// <summary>
    /// A connection string for a database of the test's own on the shared server, for a test that needs one
    /// no other test writes to. The database is created by whoever migrates it first.
    /// </summary>
    internal string ConnectionStringFor(string database) => _sql.ConnectionStringFor(database);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await Catalog.DisposeAsync();
        await Bookings.DisposeAsync();
        await Identity.DisposeAsync();

        await _rabbit.DisposeAsync();
        await _sql.DisposeAsync();
    }

    private void SetEnvironment()
    {
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__" + CatalogInfrastructureExtensions.DatabaseName,
            _sql.ConnectionStringFor(CatalogInfrastructureExtensions.DatabaseName));
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__" + BookingsInfrastructureExtensions.DatabaseName,
            _sql.ConnectionStringFor(BookingsInfrastructureExtensions.DatabaseName));
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__" + IdentityPersistenceExtensions.DatabaseName,
            _sql.ConnectionStringFor(IdentityPersistenceExtensions.DatabaseName));
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__" + RabbitMqServiceCollectionExtensions.ConnectionName, _rabbit.ConnectionString);

        Environment.SetEnvironmentVariable("Jwt__Issuer", TestJwt.Issuer);
        Environment.SetEnvironmentVariable("Jwt__Audience", TestJwt.Audience);
        Environment.SetEnvironmentVariable("Jwt__SigningKey", TestJwt.SigningKey);

        // The background publisher would race this tier's assertions ("the row is still unprocessed");
        // Catalog/OutboxTests.cs pumps IOutboxProcessor by hand instead. Bookings and Identity ignore this key.
        Environment.SetEnvironmentVariable("Messaging__RunOutboxPublisher", "false");

        Environment.SetEnvironmentVariable("HealthChecks__Expose", "true");
        Environment.SetEnvironmentVariable("OpenApi__Expose", "true");
    }
}
