using FluentValidation;
using QubicaCinema.Bookings.Domain.Bookings;

namespace QubicaCinema.Bookings.Api.Bookings;

/// <summary>
/// Checks what can be decided from the body alone, and answers 422 with every problem at once.
/// </summary>
/// <remarks>
/// The three layers of validation, stated once: the serializer rejects what is not a request at all (400);
/// this rejects a request that is well formed but impossible on its face (422); the model rejects what can
/// only be decided against the database — an unknown screening, a seat already taken (404 or 409).
/// </remarks>
internal sealed class CreateBookingRequestValidator : AbstractValidator<CreateBookingRequest>
{
    public CreateBookingRequestValidator()
    {
        RuleFor(request => request.Items)
            .NotEmpty()
            .Must(items => items.Count <= Booking.MaxScreenings)
            .WithMessage($"A booking may span at most {Booking.MaxScreenings} screenings.");

        RuleFor(request => request.Items)
            .Must(items => items.Select(item => item.ScreeningId).Distinct().Count() == items.Count)
            .When(request => request.Items is not null)
            .WithMessage("Each screening may appear only once; put all of its seats in one item.");

        RuleForEach(request => request.Items).SetValidator(new BookingItemRequestValidator());
    }
}
