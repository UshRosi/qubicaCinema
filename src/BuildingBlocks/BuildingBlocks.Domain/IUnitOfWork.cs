namespace QubicaCinema.BuildingBlocks.Domain;

/// <summary>
/// The transaction boundary of one use case: every change made through the repositories is committed
/// together, or none of it is.
/// </summary>
/// <remarks>
/// Declared here and implemented in Infrastructure by the <c>DbContext</c> itself, so that Application code
/// can commit without referencing EF Core. Registration matters: resolving it must return the very same
/// <c>DbContext</c> instance the repositories use, or the commit would save an empty change tracker.
/// </remarks>
public interface IUnitOfWork
{
    /// <summary>Commits everything tracked in the current scope.</summary>
    /// <returns>The number of state entries written.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
