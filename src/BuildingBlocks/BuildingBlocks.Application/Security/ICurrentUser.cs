namespace QubicaCinema.BuildingBlocks.Application.Security;

/// <summary>
/// Who is making the current request, as far as a use case needs to know.
/// </summary>
/// <remarks>
/// The seam that keeps <c>HttpContext</c> out of the Application layer: a use case asks "who is this and
/// what may they do?" and never reads a header or a claim itself. That is also what makes ownership rules
/// testable with a substitute, and what lets the source of identity change — a stand-in header now, a
/// validated JWT from chapter 5 — without a single use case noticing.
/// </remarks>
public interface ICurrentUser
{
    /// <summary>The caller's user id, as issued by the Identity service.</summary>
    Guid UserId { get; }

    /// <summary>Whether the caller acts for the cinema rather than for themselves.</summary>
    bool IsAdministrator { get; }
}
