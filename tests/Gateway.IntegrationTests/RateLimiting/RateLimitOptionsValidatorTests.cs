using QubicaCinema.Gateway.RateLimiting;

namespace QubicaCinema.Gateway.IntegrationTests.RateLimiting;

public sealed class RateLimitOptionsValidatorTests
{
    private readonly RateLimitOptionsValidator _validator = new();

    [Fact]
    public void The_defaults_are_valid()
    {
        _validator.Validate(name: null, new RateLimitOptions()).Succeeded.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0, 10, 100, 10)]
    [InlineData(100, 0, 10, 10)]
    [InlineData(100, 10, 0, 10)]
    [InlineData(100, 10, 10, 0)]
    public void A_non_positive_limit_or_window_is_rejected(
        int permits,
        int windowSeconds,
        int bookingPermits,
        int bookingWindowSeconds)
    {
        var options = new RateLimitOptions
        {
            PermitLimit = permits,
            Window = TimeSpan.FromSeconds(windowSeconds),
            BookingCreationPermitLimit = bookingPermits,
            BookingCreationWindow = TimeSpan.FromSeconds(bookingWindowSeconds),
        };

        _validator.Validate(name: null, options).Failed.ShouldBeTrue();
    }

    [Theory]
    [InlineData(100, 100)]
    [InlineData(100, 101)]
    public void A_booking_budget_that_is_not_stricter_than_the_global_one_is_rejected(int permits, int bookingPermits)
    {
        var options = new RateLimitOptions { PermitLimit = permits, BookingCreationPermitLimit = bookingPermits };

        var result = _validator.Validate(name: null, options);

        result.Failed.ShouldBeTrue();
        result.Failures.ShouldHaveSingleItem().ShouldContain("stricter");
    }
}
