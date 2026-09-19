using FluentValidation;

namespace QubicaCinema.Catalog.Api.Screenings;

/// <summary>A cancellation reason is optional, but it is not a place to put an essay.</summary>
internal sealed class CancelScreeningRequestValidator : AbstractValidator<CancelScreeningRequest>
{
    public CancelScreeningRequestValidator() => RuleFor(request => request.Reason).MaximumLength(500);
}
