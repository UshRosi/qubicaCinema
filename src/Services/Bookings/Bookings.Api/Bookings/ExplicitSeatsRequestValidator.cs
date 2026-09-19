using FluentValidation;
using QubicaCinema.Bookings.Domain.ValueObjects;

namespace QubicaCinema.Bookings.Api.Bookings;

/// <summary>Named seats: at least one, at most the per-screening limit, and none twice.</summary>
internal sealed class ExplicitSeatsRequestValidator : AbstractValidator<ExplicitSeatsRequest>
{
    public ExplicitSeatsRequestValidator()
    {
        RuleFor(selection => selection.SeatIds)
            .NotEmpty()
            .Must(seatIds => seatIds.Count <= SeatCount.Max)
            .WithMessage($"At most {SeatCount.Max} seats can be booked at one screening.")
            .Must(seatIds => seatIds.Distinct().Count() == seatIds.Count)
            .WithMessage("The same seat cannot be requested twice.");

        RuleForEach(selection => selection.SeatIds).NotEmpty();
    }
}
