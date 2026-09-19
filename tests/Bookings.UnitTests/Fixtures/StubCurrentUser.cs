using QubicaCinema.BuildingBlocks.Application.Security;

namespace QubicaCinema.Bookings.UnitTests.Fixtures;

/// <summary>A caller of the tests' choosing: a customer, or an administrator.</summary>
internal sealed record StubCurrentUser(Guid UserId, bool IsAdministrator = false) : ICurrentUser
{
    public static StubCurrentUser Customer() => new(Guid.CreateVersion7());

    public static StubCurrentUser Administrator() => new(Guid.CreateVersion7(), IsAdministrator: true);
}
