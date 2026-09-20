namespace QubicaCinema.Gateway.IntegrationTests.Fixtures;

/// <summary>Fixed identifiers, so a path in a test reads as a path.</summary>
internal static class TestIds
{
    internal const string Screening = "0199a0c0-0000-7000-8000-000000000001";
    internal const string Movie = "0199a0c0-0000-7000-8000-000000000002";
    internal const string Booking = "0199a0c0-0000-7000-8000-000000000003";
    internal const string BookingItem = "0199a0c0-0000-7000-8000-000000000004";

    internal static readonly Guid Customer = new("0199a0c0-0000-7000-8000-0000000000c1");
    internal static readonly Guid OtherCustomer = new("0199a0c0-0000-7000-8000-0000000000c2");
    internal static readonly Guid Administrator = new("0199a0c0-0000-7000-8000-0000000000a1");
}
