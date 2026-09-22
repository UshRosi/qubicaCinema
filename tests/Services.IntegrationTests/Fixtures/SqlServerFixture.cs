using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace QubicaCinema.Services.IntegrationTests.Fixtures;

/// <summary>
/// One SQL Server container for the whole assembly, holding the three services' databases.
/// </summary>
/// <remarks>
/// The AppHost uses the same image, so this is the same server every other tier of this solution runs
/// against. There is no per-database container: each service gets its own logical database on the one
/// instance, created by that service's own <c>Database.MigrateAsync()</c> the first time its host is
/// touched — the module has no "create a database" overload, and none is needed.
/// </remarks>
internal sealed class SqlServerFixture : IAsyncDisposable
{
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    /// <summary>Starts the container. Call once, before building any host that needs a database.</summary>
    internal Task StartAsync(CancellationToken cancellationToken) => _container.StartAsync(cancellationToken);

    /// <summary>A connection string for the given logical database on the shared server.</summary>
    internal string ConnectionStringFor(string database)
    {
        var builder = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = database,
        };

        return builder.ConnectionString;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _container.DisposeAsync();
}
