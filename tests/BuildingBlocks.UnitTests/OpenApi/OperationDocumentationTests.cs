using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using QubicaCinema.BuildingBlocks.Api.Concurrency;
using QubicaCinema.BuildingBlocks.Api.Idempotency;
using QubicaCinema.BuildingBlocks.Api.OpenApi;
using QubicaCinema.BuildingBlocks.Api.Validation;
using QubicaCinema.BuildingBlocks.UnitTests.OpenApi.Fixtures;

namespace QubicaCinema.BuildingBlocks.UnitTests.OpenApi;

/// <summary>
/// What the endpoint filters do and a Minimal API signature cannot say — headers, response headers and
/// examples — reaches the document, through the framework's own per-endpoint hook.
/// </summary>
public sealed class OperationDocumentationTests
{
    private static readonly Guid OrderId = Guid.Parse("0199a0c0-0000-7000-8000-000000000001");

    [Fact]
    public async Task Should_add_a_required_header_to_an_endpoint_that_binds_nothing_but_a_body()
    {
        // The shape of POST /bookings: a body and no other parameter, so the operation has no parameter list.
        await using OpenApiTestHost host = await OpenApiTestHost.StartAsync(endpoints =>
            endpoints.MapPost("/orders", (SampleOrder order) => TypedResults.Ok())
                .WithRequiredHeader("Idempotency-Key", "A key."));

        JsonNode operation = await host.OperationAsync("post", "/orders");

        JsonNode header = operation["parameters"]!.AsArray().ShouldHaveSingleItem()!;
        header["name"]!.GetValue<string>().ShouldBe("Idempotency-Key");
        header["in"]!.GetValue<string>().ShouldBe("header");
        header["required"]!.GetValue<bool>().ShouldBeTrue();
        header["description"]!.GetValue<string>().ShouldBe("A key.");
    }

    [Fact]
    public async Task Should_make_a_header_the_handler_binds_as_optional_required_without_listing_it_twice()
    {
        // The handlers bind If-Match as a nullable string on purpose, so the framework calls it optional.
        await using OpenApiTestHost host = await OpenApiTestHost.StartAsync(endpoints =>
            endpoints.MapPut(
                    "/orders/{id:guid}",
                    (Guid id, [FromHeader(Name = "If-Match")] string? ifMatch) => TypedResults.NoContent())
                .RequiringIfMatch());

        JsonNode operation = await host.OperationAsync("put", "/orders/{id}");

        JsonNode ifMatch = operation["parameters"]!.AsArray()
            .Where(parameter => parameter!["name"]!.GetValue<string>() == HeaderNames.IfMatch)
            .ShouldHaveSingleItem()!;
        ifMatch["required"]!.GetValue<bool>().ShouldBeTrue();
    }

    [Fact]
    public async Task Should_document_an_etag_and_a_location_on_the_responses_that_carry_them()
    {
        await using OpenApiTestHost host = await OpenApiTestHost.StartAsync(endpoints =>
        {
            endpoints.MapGet("/orders/{id:guid}", (Guid id) => TypedResults.Ok(new SampleOrder(id, SampleStatus.Pending, [])))
                .WithETagHeader();
            endpoints.MapPost("/orders", (SampleOrder order) => TypedResults.Created("/orders/1"))
                .WithLocationHeader();
        });

        JsonNode read = await host.OperationAsync("get", "/orders/{id}");
        JsonNode create = await host.OperationAsync("post", "/orders");

        read["responses"]!["200"]!["headers"]!["ETag"].ShouldNotBeNull();
        create["responses"]!["201"]!["headers"]!["Location"].ShouldNotBeNull();
    }

    [Fact]
    public async Task Should_write_a_request_example_with_the_services_own_names_and_enum_strings()
    {
        SampleOrder order = new(OrderId, SampleStatus.Shipped, []);

        await using OpenApiTestHost host = await OpenApiTestHost.StartAsync(endpoints =>
            endpoints.MapPost("/orders", (SampleOrder body) => TypedResults.Ok())
                .WithRequestExample("shipped", "An order that shipped", order));

        JsonNode operation = await host.OperationAsync("post", "/orders");

        JsonNode example = operation["requestBody"]!["content"]!["application/json"]!["examples"]!["shipped"]!;
        example["summary"]!.GetValue<string>().ShouldBe("An order that shipped");
        example["value"]!["orderId"]!.GetValue<Guid>().ShouldBe(OrderId);
        example["value"]!["status"]!.GetValue<string>().ShouldBe("Shipped");
    }

    [Fact]
    public async Task Should_carry_the_discriminator_of_a_polymorphic_value_nested_in_the_example()
    {
        SampleOrder order = new(OrderId, SampleStatus.Pending, [new SampleLineByQuantity(2), new SampleLineByCode("A7")]);

        await using OpenApiTestHost host = await OpenApiTestHost.StartAsync(endpoints =>
            endpoints.MapPost("/orders", (SampleOrder body) => TypedResults.Ok())
                .WithRequestExample("mixed", "Both kinds of line", order));

        JsonNode operation = await host.OperationAsync("post", "/orders");

        JsonArray lines = operation["requestBody"]!["content"]!["application/json"]!["examples"]!["mixed"]!["value"]!["lines"]!.AsArray();
        lines[0]!["mode"]!.GetValue<string>().ShouldBe("quantity");
        lines[0]!["quantity"]!.GetValue<int>().ShouldBe(2);
        lines[1]!["mode"]!.GetValue<string>().ShouldBe("code");
    }

    [Fact]
    public async Task Should_write_several_problem_examples_under_one_status()
    {
        ProblemDetails taken = new() { Type = "urn:test:taken", Status = 409, Title = "Taken" };
        taken.Extensions["unavailableSeatIds"] = new[] { OrderId };
        ProblemDetails full = new() { Type = "urn:test:taken", Status = 409, Title = "Full" };
        full.Extensions["available"] = 6;

        await using OpenApiTestHost host = await OpenApiTestHost.StartAsync(endpoints =>
            endpoints.MapPost("/orders", (SampleOrder body) => TypedResults.Ok())
                .ProducesProblem(StatusCodes.Status409Conflict)
                .WithResponseExample(StatusCodes.Status409Conflict, "taken", "Somebody was first", taken)
                .WithResponseExample(StatusCodes.Status409Conflict, "full", "No room left", full));

        JsonNode operation = await host.OperationAsync("post", "/orders");

        JsonNode problem = operation["responses"]!["409"]!["content"]!["application/problem+json"]!;
        problem["schema"].ShouldNotBeNull("the declared ProblemDetails schema must survive the examples");
        problem["examples"]!["taken"]!["value"]!["unavailableSeatIds"]![0]!.GetValue<Guid>().ShouldBe(OrderId);
        problem["examples"]!["full"]!["value"]!["available"]!.GetValue<int>().ShouldBe(6);
    }

    [Fact]
    public async Task Should_keep_validations_error_map_and_the_idempotency_examples_under_the_same_422()
    {
        // The order POST /bookings adds them in: validation first, then the idempotency key.
        await using OpenApiTestHost host = await OpenApiTestHost.StartAsync(endpoints =>
            endpoints.MapPost("/orders", (SampleOrder body) => TypedResults.Ok())
                .ValidatingBody<SampleOrder>()
                .RequiringIdempotencyKey<SampleOrder>());

        JsonNode operation = await host.OperationAsync("post", "/orders");

        JsonNode unprocessable = operation["responses"]!["422"]!["content"]!["application/problem+json"]!;
        unprocessable["schema"]!["$ref"]!.GetValue<string>().ShouldEndWith("HttpValidationProblemDetails");
        unprocessable["examples"]!["idempotency-key-reuse"].ShouldNotBeNull();

        operation["parameters"]!.AsArray()
            .Single(parameter => parameter!["name"]!.GetValue<string>() == "Idempotency-Key")!["required"]!
            .GetValue<bool>().ShouldBeTrue();
    }
}
