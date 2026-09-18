using QubicaCinema.BuildingBlocks.Application.Handlers;
using QubicaCinema.BuildingBlocks.Domain;
using QubicaCinema.Catalog.Application.Abstractions.Repositories;
using QubicaCinema.Catalog.Domain.Auditoriums;
using QubicaCinema.Catalog.Domain.Exceptions;

namespace QubicaCinema.Catalog.Application.Auditoriums.CreateAuditorium;

/// <inheritdoc cref="CreateAuditoriumCommand" />
public sealed class CreateAuditoriumHandler(IAuditoriumRepository auditoriums, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateAuditoriumCommand, Guid>
{
    /// <inheritdoc />
    /// <exception cref="AuditoriumNameAlreadyUsedException">Another room already has that name.</exception>
    public async Task<Guid> HandleAsync(CreateAuditoriumCommand command, CancellationToken cancellationToken)
    {
        // Checked here so the usual case gets a clear answer rather than a unique-index violation. It is
        // not the guarantee — two simultaneous requests both pass this — which is why the index exists and
        // why its violation is translated back into this same exception.
        if (await auditoriums.NameIsTakenAsync(command.Name, cancellationToken))
        {
            throw new AuditoriumNameAlreadyUsedException(command.Name);
        }

        var auditorium = Auditorium.Create(command.Name, command.RowCount, command.SeatsPerRow);

        auditoriums.Add(auditorium);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return auditorium.Id;
    }
}
