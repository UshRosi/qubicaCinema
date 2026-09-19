using QubicaCinema.BuildingBlocks.Application.Security;

namespace QubicaCinema.Bookings.Api.Security;

/// <summary>
/// A stand-in for authentication until chapter 5: the caller says who they are in two headers.
/// </summary>
/// <remarks>
/// Deliberately temporary and deliberately obvious. Chapter 5 replaces this class with one that reads a
/// validated JWT, and because the use cases only ever see <see cref="ICurrentUser"/>, nothing else changes.
/// Until then it lets the ownership rules be exercised by hand: <c>X-User-Id</c> picks the customer
/// (a fixed demo customer when absent) and <c>X-User-Role: Admin</c> acts for the cinema. It trusts the
/// caller completely, which is exactly why it must not outlive the chapter.
/// </remarks>
// TODO(chapter 5): delete this class and register an ICurrentUser that reads the validated JWT principal
// (the "sub" and role claims). ICurrentUser itself is permanent; only this header-based stand-in goes.
internal sealed class HeaderCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    /// <summary>Who the caller is when they do not say.</summary>
    internal static readonly Guid DemoCustomerId = new("0199a0c0-0000-7000-8000-00000000c0de");

    private const string UserIdHeader = "X-User-Id";
    private const string RoleHeader = "X-User-Role";
    private const string AdministratorRole = "Admin";

    /// <inheritdoc />
    public Guid UserId => Guid.TryParse(Header(UserIdHeader), out Guid userId) ? userId : DemoCustomerId;

    /// <inheritdoc />
    public bool IsAdministrator => string.Equals(Header(RoleHeader), AdministratorRole, StringComparison.OrdinalIgnoreCase);

    private string? Header(string name) => httpContextAccessor.HttpContext?.Request.Headers[name].ToString();
}
