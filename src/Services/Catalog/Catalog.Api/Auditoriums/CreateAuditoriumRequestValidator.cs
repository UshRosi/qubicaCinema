using FluentValidation;
using QubicaCinema.BuildingBlocks.Domain.ValueObjects;
using QubicaCinema.Catalog.Domain.Auditoriums;

namespace QubicaCinema.Catalog.Api.Auditoriums;

/// <summary>Checks the grid can exist before the aggregate is asked to build it.</summary>
internal sealed class CreateAuditoriumRequestValidator : AbstractValidator<CreateAuditoriumRequest>
{
    public CreateAuditoriumRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(Auditorium.MaxNameLength);

        RuleFor(request => request.RowCount)
            .InclusiveBetween(1, SeatPosition.MaxRows)
            .WithMessage($"An auditorium must have between 1 and {SeatPosition.MaxRows} rows, because rows are lettered A to Z.");

        RuleFor(request => request.SeatsPerRow)
            .InclusiveBetween(1, SeatPosition.MaxNumber);
    }
}
