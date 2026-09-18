using QubicaCinema.BuildingBlocks.Application.Handlers;
using QubicaCinema.BuildingBlocks.Domain;
using QubicaCinema.Catalog.Application.Abstractions.Repositories;
using QubicaCinema.Catalog.Domain.Exceptions;
using QubicaCinema.Catalog.Domain.Screenings;

namespace QubicaCinema.Catalog.Application.Screenings.CancelScreening;

/// <inheritdoc cref="CancelScreeningCommand" />
/// <remarks>
/// No <c>If-Match</c> here, unlike rescheduling. Cancelling is idempotent and its outcome does not depend
/// on what the caller last read: asking twice cancels once, and a caller who has not seen the latest price
/// is not thereby wrong about wanting the screening called off.
/// </remarks>
public sealed class CancelScreeningHandler(
    IScreeningRepository screenings,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<CancelScreeningCommand, Guid>
{
    /// <inheritdoc />
    /// <exception cref="ScreeningNotFoundException">No screening has that id.</exception>
    /// <exception cref="ScreeningAlreadyStartedException">It has already begun.</exception>
    public async Task<Guid> HandleAsync(CancelScreeningCommand command, CancellationToken cancellationToken)
    {
        Screening screening = await screenings.FindAsync(command.ScreeningId, cancellationToken)
                              ?? throw new ScreeningNotFoundException(command.ScreeningId);

        screening.Cancel(clock, command.Reason);

        // A second cancellation changes nothing, so this writes nothing — and still answers 200.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return screening.Id;
    }
}
