using QubicaCinema.BuildingBlocks.Application.Handlers;
using QubicaCinema.BuildingBlocks.Domain;
using QubicaCinema.Catalog.Application.Abstractions.Repositories;
using QubicaCinema.Catalog.Domain.Exceptions;
using QubicaCinema.Catalog.Domain.Movies;
using QubicaCinema.Catalog.Domain.Screenings;
using QubicaCinema.Catalog.Domain.ValueObjects;

namespace QubicaCinema.Catalog.Application.Screenings.ScheduleScreening;

/// <inheritdoc cref="ScheduleScreeningCommand" />
/// <remarks>
/// The handler's whole job is to gather what the aggregate needs to decide: the film's running time, the
/// fact that the room exists, and the slots that room is already committed to. The decision itself — is the
/// auditorium free? — is <see cref="Screening.Schedule"/>'s, and is unit tested without any of this.
/// </remarks>
public sealed class ScheduleScreeningHandler(
    IMovieRepository movies,
    IAuditoriumRepository auditoriums,
    IScreeningRepository screenings,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<ScheduleScreeningCommand, Guid>
{
    /// <inheritdoc />
    /// <exception cref="MovieNotFoundException">No film has that id.</exception>
    /// <exception cref="AuditoriumNotFoundException">No auditorium has that id.</exception>
    /// <exception cref="OverlappingScreeningException">The room is busy during part of the slot.</exception>
    public async Task<Guid> HandleAsync(ScheduleScreeningCommand command, CancellationToken cancellationToken)
    {
        Movie movie = await movies.FindAsync(command.MovieId, cancellationToken)
                      ?? throw new MovieNotFoundException(command.MovieId);

        if (!await auditoriums.ExistsAsync(command.AuditoriumId, cancellationToken))
        {
            throw new AuditoriumNotFoundException(command.AuditoriumId);
        }

        // Ask the model how much of the day this screening would take, then ask the database only about
        // that window. Reading the auditorium's whole programme to compare three dates would be the same
        // answer at a much higher price.
        TimeSlot candidate = Screening.OccupationFor(command.StartsAt, movie.Duration);

        IReadOnlyCollection<ScheduledSlot> committed = await screenings.GetScheduleAsync(
            command.AuditoriumId,
            candidate.StartsAt,
            candidate.EndsAt,
            cancellationToken);

        var screening = Screening.Schedule(
            movie.Id,
            movie.Duration,
            command.AuditoriumId,
            command.StartsAt,
            Money.Of(command.PriceAmount, command.PriceCurrency),
            committed,
            clock);

        screenings.Add(screening);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return screening.Id;
    }
}
