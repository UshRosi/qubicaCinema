using FluentValidation;
using QubicaCinema.Bookings.Domain.ValueObjects;

namespace QubicaCinema.Bookings.Api.Bookings;

/// <summary>A number of seats within what one booking may take at one screening.</summary>
/// <remarks>The bounds are read off <see cref="SeatCount"/>, which enforces the same rule, so the two cannot drift.</remarks>
internal sealed class SeatQuantityRequestValidator : AbstractValidator<SeatQuantityRequest>
{
    public SeatQuantityRequestValidator() =>
        RuleFor(selection => selection.Quantity).InclusiveBetween(SeatCount.Min, SeatCount.Max);
}
