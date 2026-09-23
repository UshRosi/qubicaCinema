using System.Net;
using System.Text;
using QubicaCinema.Services.IntegrationTests.Fixtures;

namespace QubicaCinema.Services.IntegrationTests.Bookings;

/// <summary>
/// Proves that a booking body with parts missing, which the serializer lets through as nulls, is answered 422
/// by the validator rather than 500 by a rule that tried to count a list that is not there.
/// </summary>
/// <remarks>
/// Raw JSON rather than anonymous objects: these are bodies a client could send by mistake, and the point is
/// what the service does with exactly those bytes. None of them gets as far as the database, so the screening
/// id is an arbitrary one.
/// </remarks>
public sealed class RequestValidationTests(CinemaFixture cinema)
{
    [Theory]
    [InlineData("{}")]
    [InlineData("""{"items":null}""")]
    [InlineData("""{"items":[null]}""")]
    [InlineData("""{"items":[{"screeningId":"0199c6a2-0000-7000-8000-000000000001","selection":null}]}""")]
    [InlineData("""{"items":[{"screeningId":"0199c6a2-0000-7000-8000-000000000001","selection":{"mode":"seats"}}]}""")]
    [InlineData("""{"items":[{"screeningId":"0199c6a2-0000-7000-8000-000000000001","selection":{"mode":"seats","seatIds":null}}]}""")]
    public async Task A_booking_with_a_missing_part_is_answered_422(string body)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        HttpClient customer = cinema.NewCustomer();

        using HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/bookings")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());

        using HttpResponseMessage response = await customer.SendAsync(request, cancellationToken);

        response.StatusCode.ShouldBe((HttpStatusCode)422);
    }
}
