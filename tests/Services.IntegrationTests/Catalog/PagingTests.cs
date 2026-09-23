using System.Net;
using System.Text.Json;
using QubicaCinema.Services.IntegrationTests.Fixtures;

namespace QubicaCinema.Services.IntegrationTests.Catalog;

/// <summary>Proves that no page number, however large, reaches SQL Server as an offset it refuses.</summary>
public sealed class PagingTests(CinemaFixture cinema)
{
    [Fact]
    public async Task A_page_far_beyond_the_last_one_is_answered_with_an_empty_page()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        HttpClient anonymous = cinema.Catalog.CreateClient();

        using HttpResponseMessage response =
            await anonymous.GetAsync($"/api/v1/movies?page={int.MaxValue}&pageSize=100", cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        JsonElement page = await response.Content.ReadFromJsonAsync<JsonElement>(TestJson.Options, cancellationToken);
        page.GetProperty("items").GetArrayLength().ShouldBe(0);
    }
}
