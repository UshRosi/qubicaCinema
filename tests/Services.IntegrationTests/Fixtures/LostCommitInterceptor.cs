using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace QubicaCinema.Services.IntegrationTests.Fixtures;

/// <summary>
/// Makes one commit go through on the server and then fail on the client, the way a connection that drops
/// before the acknowledgement arrives does.
/// </summary>
/// <remarks>
/// A <see cref="TimeoutException"/> for the same reason as in <see cref="FailingSaveInterceptor"/>: the
/// retrying execution strategy treats it as transient, so it replays a unit of work whose data is already
/// committed.
/// </remarks>
internal sealed class LostCommitInterceptor : DbTransactionInterceptor
{
    private bool _loseNextCommit;

    /// <summary>Loses the acknowledgement of the next commit, and only that one.</summary>
    internal void LoseNextCommit() => _loseNextCommit = true;

    public override Task TransactionCommittedAsync(
        DbTransaction transaction,
        TransactionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        if (_loseNextCommit)
        {
            _loseNextCommit = false;
            throw new TimeoutException("Simulated transient failure: the commit went through but its acknowledgement was lost.");
        }

        return base.TransactionCommittedAsync(transaction, eventData, cancellationToken);
    }
}
