using QubicaCinema.BuildingBlocks.Domain;
using QubicaCinema.Bookings.Application.Abstractions.Repositories;
using QubicaCinema.Bookings.Domain.Bookings;
using QubicaCinema.Bookings.Domain.Exceptions;

namespace QubicaCinema.Bookings.Application.Bookings.CreateBooking;

/// <summary>
/// Commits a new booking, choosing its automatically allocated seats again if another booking took them
/// in the meantime.
/// </summary>
/// <remarks>
/// Two customers asking for "any two seats" on a half-empty screening read the same seat map, the strategy
/// picks the same best pair for both, and the unique index lets only one of them in. Failing the other
/// with a 409 would be absurd — there are plenty of seats left — so the loser composes the booking again
/// from a fresh map, and the strategy picks the next best pair.
/// <para>
/// The loop is bounded: under real contention it would otherwise keep losing, and three attempts turn a
/// transient race into a clear answer. It retries only when the taken seats were chosen automatically;
/// seats the customer named cannot be swapped for others, so that conflict goes straight back as a 409.
/// </para>
/// <para>
/// It is its own class, not a loop in the handler, because a retry needs to catch the conflict and
/// handlers in this solution do not catch; this is the single place where the rule "a race over chosen
/// seats is worth another try" lives.
/// </para>
/// </remarks>
public sealed class BookingPlacement(IBookingRepository bookings, IUnitOfWork unitOfWork)
{
    /// <summary>How many times a booking is composed and committed before a conflict is reported.</summary>
    public const int MaxAttempts = 3;

    /// <summary>Composes the booking, stages it and commits it, composing it again after a lost race.</summary>
    /// <param name="compose">Builds the booking from a fresh read of the seat maps.</param>
    /// <param name="isWorthRetrying">Whether a conflict concerns seats that can be chosen again.</param>
    /// <param name="cancellationToken">Cancels the whole placement.</param>
    /// <returns>The booking that was committed.</returns>
    /// <exception cref="SeatAlreadyBookedException">The seats were taken, and choosing again is not possible or did not help.</exception>
    public async Task<Booking> PlaceAsync(
        Func<CancellationToken, Task<Booking>> compose,
        Func<SeatAlreadyBookedException, bool> isWorthRetrying,
        CancellationToken cancellationToken)
    {
        for (int attempt = 1; ; attempt++)
        {
            Booking booking = await compose(cancellationToken);
            bookings.Add(booking);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);

                return booking;
            }
            catch (SeatAlreadyBookedException conflict) when (attempt < MaxAttempts && isWorthRetrying(conflict))
            {
                // The losing booking is still staged; without this, the next attempt would commit both.
                bookings.Discard(booking);
            }
        }
    }
}
