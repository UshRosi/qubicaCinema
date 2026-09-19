namespace QubicaCinema.BuildingBlocks.Application.Handlers;

/// <summary>
/// One use case that changes something.
/// </summary>
/// <remarks>
/// A shape, not a pipeline. There is no dispatcher and no <c>ISender</c> in this solution: the concrete
/// handler class is registered in DI and injected straight into the endpoint that needs it, so "find the
/// code that runs" is go-to-definition rather than a runtime lookup through a registry. The interface earns
/// its place by naming the contract every use case follows and by making a handler trivial to substitute in
/// a test — nothing resolves a service <em>through</em> it. It lives in a building block because every
/// service writes its use cases to the same contract.
/// </remarks>
/// <typeparam name="TCommand">What the caller asked for.</typeparam>
/// <typeparam name="TResult">What the caller gets back.</typeparam>
public interface ICommandHandler<in TCommand, TResult>
{
    /// <summary>Runs the use case.</summary>
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
