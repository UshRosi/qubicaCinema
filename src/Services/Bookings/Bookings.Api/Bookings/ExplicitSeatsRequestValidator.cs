using FluentValidation;
using QubicaCinema.Bookings.Domain.ValueObjects;

namespace QubicaCinema.Bookings.Api.Bookings;

/// <summary>Named seats: at least one, at most the per-screening limit, and none twice.</summary>
/// <remarks><c>{"mode":"seats"}</c> with no <c>seatIds</c> binds a null list, which must stop at <c>NotEmpty</c>.</remarks>
internal sealed class ExplicitSeatsRequestValidator : AbstractValidator<ExplicitSeatsRequest>
{
    public ExplicitSeatsRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(selection => selection.SeatIds)
            .NotEmpty()
            .Must(seatIds => seatIds.Count <= SeatCount.Max)
            .WithMessage($"At most {SeatCount.Max} seats can be booked at one screening.")
            .Must(seatIds => seatIds.Distinct().Count() == seatIds.Count)
            .WithMessage("The same seat cannot be requested twice.");

        RuleForEach(selection => selection.SeatIds).NotEmpty();
    }
}
