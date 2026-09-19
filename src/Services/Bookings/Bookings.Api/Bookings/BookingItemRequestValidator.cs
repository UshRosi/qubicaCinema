using FluentValidation;

namespace QubicaCinema.Bookings.Api.Bookings;

/// <summary>Checks one screening's entry, dispatching to the rules of whichever selection it carries.</summary>
internal sealed class BookingItemRequestValidator : AbstractValidator<BookingItemRequest>
{
    public BookingItemRequestValidator()
    {
        RuleFor(item => item.ScreeningId).NotEmpty();

        RuleFor(item => item.Selection)
            .NotNull()
            .SetInheritanceValidator(selection =>
            {
                selection.Add(new ExplicitSeatsRequestValidator());
                selection.Add(new SeatQuantityRequestValidator());
            });
    }
}
