using FluentValidation;
using QubicaCinema.Catalog.Api.Validation;

namespace QubicaCinema.Catalog.Api.Screenings;

/// <summary>Checks a new time and price before the model is asked to reschedule.</summary>
internal sealed class RescheduleScreeningRequestValidator : AbstractValidator<RescheduleScreeningRequest>
{
    public RescheduleScreeningRequestValidator()
    {
        RuleFor(request => request.PriceAmount).GreaterThanOrEqualTo(0);
        RuleFor(request => request.PriceCurrency).MustBeCurrencyCode();
    }
}
