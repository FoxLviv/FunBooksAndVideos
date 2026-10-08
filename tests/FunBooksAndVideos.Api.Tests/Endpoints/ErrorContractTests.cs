using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using FunBooksAndVideos.Api.Tests.Infrastructure;
using FunBooksAndVideos.Infrastructure.Seeding;

namespace FunBooksAndVideos.Api.Tests.Endpoints;

/// <summary>Every error is an RFC 9457 problem document with a stable <c>code</c> and a <c>traceId</c>.</summary>
public sealed class ErrorContractTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public ErrorContractTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Validation_failures_are_problem_documents_with_field_errors()
    {
        var response = await _client.PostJsonAsync("/api/v1/purchase-orders", new { lines = Array.Empty<object>() });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be(HttpJson.ProblemContentType);
        var problem = await response.ReadProblemAsync();
        problem.Status.Should().Be(400);
        problem.Title.Should().Be("The request is malformed.");
        problem.Instance.Should().Be("/api/v1/purchase-orders");
        problem.Code().Should().Be("request.invalid");
        problem.Errors.Keys.Should().Contain("customerId").And.Contain("lines");
        problem.Extensions.Should().ContainKey("traceId");
    }

    [Fact]
    public async Task Semantic_failures_are_problem_documents_with_a_domain_code_and_detail()
    {
        var response = await _client.PostJsonAsync("/api/v1/purchase-orders", Requests.Order(DemoData.JaneDoeCustomerId, null, Requests.Membership("Premium"), Requests.Membership("VideoClub")));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        response.Content.Headers.ContentType!.MediaType.Should().Be(HttpJson.ProblemContentType);
        var problem = await response.ReadProblemAsync();
        problem.Status.Should().Be(422);
        problem.Title.Should().Be("The request violates a domain rule.");
        problem.Detail.Should().NotBeNullOrWhiteSpace();
        problem.Code().Should().Be("purchase_order.memberships.overlap");
        problem.Extensions.Should().ContainKey("traceId");
    }

    [Fact]
    public async Task Use_case_validation_failures_carry_request_member_names()
    {
        var response = await _client.PostJsonAsync("/api/v1/purchase-orders", Requests.Order(DemoData.JaneDoeCustomerId, null, Requests.Product(424242)));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var problem = await response.ReadProblemAsync();
        problem.Code().Should().Be("validation.failed");
        problem.Errors.Should().ContainKey("lines[0].productId").WhoseValue.Single().Should().Be("Product 424242 does not exist.");
    }

    [Fact]
    public async Task Unknown_routes_are_problem_documents_too()
    {
        var response = await _client.GetAsync("/api/v1/nothing-here");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be(HttpJson.ProblemContentType);
        var problem = await response.ReadProblemAsync();
        problem.Status.Should().Be(404);
        problem.Code().Should().Be("resource.not_found");
        problem.Extensions.Should().ContainKey("traceId");
    }

    [Fact]
    public async Task Unsupported_media_types_carry_a_code()
    {
        var response = await _client.PostAsync("/api/v1/purchase-orders", new StringContent("customerId=1", System.Text.Encoding.UTF8, "text/plain"));

        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
        response.Content.Headers.ContentType!.MediaType.Should().Be(HttpJson.ProblemContentType);
        var problem = await response.ReadProblemAsync();
        problem.Status.Should().Be(415);
        problem.Code().Should().Be("media_type.unsupported");
    }

    [Fact]
    public async Task Framework_errors_stay_problem_json_when_the_client_does_not_accept_json()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/nothing-here");
        request.Headers.Accept.ParseAdd("text/html");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be(HttpJson.ProblemContentType);
        var problem = await response.ReadProblemAsync();
        problem.Status.Should().Be(404);
        problem.Title.Should().Be("Not Found");
        problem.Code().Should().Be("resource.not_found");
        problem.Extensions.Should().ContainKey("traceId");
    }

    [Fact]
    public async Task Field_errors_survive_a_non_json_accept_header()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/purchase-orders")
        {
            Content = JsonContent.Create(Requests.Order(999_999_999, null, Requests.Product(DemoData.FirstAidVideoId)), options: HttpJson.Options),
        };
        request.Headers.Accept.ParseAdd("text/html");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        response.Content.Headers.ContentType!.MediaType.Should().Be(HttpJson.ProblemContentType);
        var problem = await response.ReadProblemAsync();
        problem.Code().Should().Be("validation.failed");
        problem.Errors.Should().ContainKey("customerId");
        problem.Extensions.Should().ContainKey("traceId");
    }
}
