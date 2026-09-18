using FluentValidation;
using QubicaCinema.Catalog.Api.Validation;

namespace QubicaCinema.Catalog.Api.Screenings;

/// <summary>Checks a screening request before the model is asked to schedule it.</summary>
/// <remarks>
/// "Not in the past" is not checked here. It depends on the clock rather than on the payload, so it is the
/// model's call and comes back as 422 from <c>ScreeningInThePastException</c> — the same status this filter
/// would have produced, decided in the one place that owns the rule.
/// </remarks>
internal sealed class ScheduleScreeningRequestValidator : AbstractValidator<ScheduleScreeningRequest>
{
    public ScheduleScreeningRequestValidator()
    {
        RuleFor(request => request.MovieId).NotEmpty();
        RuleFor(request => request.AuditoriumId).NotEmpty();
        RuleFor(request => request.PriceAmount).GreaterThanOrEqualTo(0);
        RuleFor(request => request.PriceCurrency).MustBeCurrencyCode();
    }
}
