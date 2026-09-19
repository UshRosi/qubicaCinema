namespace QubicaCinema.BuildingBlocks.Application.Handlers;

/// <summary>
/// One use case that only reads. Separated from <see cref="ICommandHandler{TCommand,TResult}"/> so that a
/// reader can tell at the type level which endpoints can change the database.
/// </summary>
/// <typeparam name="TQuery">What is being asked.</typeparam>
/// <typeparam name="TResult">The answer.</typeparam>
public interface IQueryHandler<in TQuery, TResult>
{
    /// <summary>Answers the query.</summary>
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
