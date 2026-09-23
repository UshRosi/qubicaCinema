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
/// <para>
/// The serializer lets a missing or null list through, and null items inside it, so every rule here has to
/// expect them. A rule stops at its first failure: a null list fails <c>NotEmpty</c> and never reaches the
/// checks that count it.
/// </para>
/// </remarks>
internal sealed class CreateBookingRequestValidator : AbstractValidator<CreateBookingRequest>
{
    public CreateBookingRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.Items)
            .NotEmpty()
            .Must(items => items.Count <= Booking.MaxScreenings)
            .WithMessage($"A booking may span at most {Booking.MaxScreenings} screenings.")
            .Must(HaveOneItemPerScreening)
            .WithMessage("Each screening may appear only once; put all of its seats in one item.");

        RuleForEach(request => request.Items)
            .NotNull()
            .SetValidator(new BookingItemRequestValidator());
    }

    /// <summary>Whether no screening appears twice. A null item is reported by the per-item rule, not here.</summary>
    private static bool HaveOneItemPerScreening(IReadOnlyList<BookingItemRequest> items)
    {
        List<Guid> screeningIds = [.. items.Where(item => item is not null).Select(item => item.ScreeningId)];

        return screeningIds.Distinct().Count() == screeningIds.Count;
    }
}
