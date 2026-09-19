using System.Text.Json.Serialization;
using QubicaCinema.Bookings.Domain.SeatAllocation;

namespace QubicaCinema.Bookings.Api.Bookings;

/// <summary>
/// How the seats at one screening are chosen: <c>{"mode":"seats","seatIds":[…]}</c> or
/// <c>{"mode":"quantity","quantity":2}</c>.
/// </summary>
/// <remarks>
/// "Seats <em>or</em> a quantity" is enforced by the serializer, not by a validation rule: a body is one
/// case or the other, never both and never neither, and an unknown <c>mode</c> fails to deserialize and
/// becomes a 400 before any code of ours runs. Three notes worth keeping next to the attributes:
/// <list type="bullet">
/// <item><description>an abstract base is a deliberate exception to "prefer composition": this is a closed
/// two-case union mirroring <see cref="SeatSelection"/>, not a way to share behaviour;</description></item>
/// <item><description>System.Text.Json used to require the discriminator to be the first property. The
/// service turns on <c>AllowOutOfOrderMetadataProperties</c>, so <c>mode</c> may appear anywhere — clients
/// should not have to care about property order;</description></item>
/// <item><description>OpenAPI describes this as a <c>oneOf</c> with a discriminator, which is exactly the
/// contract a client generator needs.</description></item>
/// </list>
/// Each case converts itself into the domain's selection, so there is no switch over request types.
/// </remarks>
[JsonPolymorphic(
    TypeDiscriminatorPropertyName = "mode",
    UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization)]
[JsonDerivedType(typeof(ExplicitSeatsRequest), "seats")]
[JsonDerivedType(typeof(SeatQuantityRequest), "quantity")]
public abstract record SeatSelectionRequest
{
    /// <summary>The domain's version of this selection. Called only once the request has been validated.</summary>
    internal abstract SeatSelection ToSelection();
}
