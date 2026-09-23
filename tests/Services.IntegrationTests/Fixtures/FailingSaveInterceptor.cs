using Microsoft.EntityFrameworkCore.Diagnostics;

namespace QubicaCinema.Services.IntegrationTests.Fixtures;

/// <summary>
/// Makes one save fail before anything reaches the server, the way a dropped connection does.
/// </summary>
/// <remarks>
/// A <see cref="TimeoutException"/> because SQL Server's retrying execution strategy treats it as transient,
/// and a real <c>SqlException</c> cannot be constructed outside the driver.
/// </remarks>
internal sealed class FailingSaveInterceptor : SaveChangesInterceptor
{
    private bool _failNextSave;

    /// <summary>Fails the next save, and only that one.</summary>
    internal void FailNextSave() => _failNextSave = true;

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (_failNextSave)
        {
            _failNextSave = false;
            throw new TimeoutException("Simulated transient failure: the save never reached the server.");
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
