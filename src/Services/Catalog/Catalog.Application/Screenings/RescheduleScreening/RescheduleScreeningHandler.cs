using QubicaCinema.BuildingBlocks.Application.Handlers;
using QubicaCinema.BuildingBlocks.Domain.ValueObjects;
using QubicaCinema.BuildingBlocks.Domain;
using QubicaCinema.Catalog.Application.Abstractions.Repositories;
using QubicaCinema.Catalog.Domain.Exceptions;
using QubicaCinema.Catalog.Domain.Movies;
using QubicaCinema.Catalog.Domain.Screenings;
using QubicaCinema.Catalog.Domain.ValueObjects;

namespace QubicaCinema.Catalog.Application.Screenings.RescheduleScreening;

/// <inheritdoc cref="RescheduleScreeningCommand" />
public sealed class RescheduleScreeningHandler(
    IScreeningRepository screenings,
    IMovieRepository movies,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<RescheduleScreeningCommand, Guid>
{
    /// <inheritdoc />
    /// <exception cref="ScreeningNotFoundException">No screening has that id.</exception>
    /// <exception cref="MovieNotFoundException">Its film has since been removed from the catalogue.</exception>
    /// <exception cref="OverlappingScreeningException">The room is busy during part of the new slot.</exception>
    public async Task<Guid> HandleAsync(RescheduleScreeningCommand command, CancellationToken cancellationToken)
    {
        Screening screening = await screenings.FindAsync(command.ScreeningId, cancellationToken)
                              ?? throw new ScreeningNotFoundException(command.ScreeningId);

        // The film's running time is read again rather than reused from the old slot: if it was corrected
        // in the meantime, the new slot must reflect the film as it is now.
        Movie movie = await movies.FindAsync(screening.MovieId, cancellationToken)
                      ?? throw new MovieNotFoundException(screening.MovieId);

        TimeSlot candidate = Screening.OccupationFor(command.StartsAt, movie.Duration);

        IReadOnlyCollection<ScheduledSlot> committed = await screenings.GetScheduleAsync(
            screening.AuditoriumId,
            candidate.StartsAt,
            candidate.EndsAt,
            cancellationToken);

        screening.Reschedule(
            movie.Duration,
            command.StartsAt,
            Money.Of(command.PriceAmount, command.PriceCurrency),
            committed,
            clock);

        screenings.Update(screening, command.ExpectedVersion);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return screening.Id;
    }
}
