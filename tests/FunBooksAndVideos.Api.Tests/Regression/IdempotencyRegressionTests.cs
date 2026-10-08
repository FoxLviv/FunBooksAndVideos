using System.Net;
using System.Text.Json;
using FluentAssertions;
using FunBooksAndVideos.Api.Controllers;
using FunBooksAndVideos.Api.Tests.Infrastructure;
using FunBooksAndVideos.Application.Dtos;
using FunBooksAndVideos.Application.PurchaseOrders.SubmitPurchaseOrder;
using FunBooksAndVideos.Domain.Customers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace FunBooksAndVideos.Api.Tests.Regression;

/// <summary>
/// <c>Idempotency-Key</c> makes order submission safe to retry: the same key and body replay the original result
/// without creating a second order, the same key with another body is a conflict.
/// </summary>
[Trait("Category", "Regression")]
public sealed class IdempotencyRegressionTests : IClassFixture<ApiFactory>
{
    private const string Url = "/api/v1/purchase-orders";

    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public IdempotencyRegressionTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static string NewKey() => $"test-{Guid.NewGuid():N}";

    [Fact]
    public async Task Repeating_a_request_with_the_same_key_replays_the_original_order_and_creates_nothing_new()
    {
        var customerId = await _client.CreateCustomerAsync();
        var key = NewKey();
        var body = Requests.Order(customerId, null, Requests.Product(2), Requests.Membership("BookClub"));

        var firstResponse = await _client.PostJsonWithIdempotencyKeyAsync(Url, body, key);
        var secondResponse = await _client.PostJsonWithIdempotencyKeyAsync(Url, body, key);

        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        firstResponse.Headers.Contains("Idempotent-Replayed").Should().BeFalse();
        secondResponse.Headers.GetValues("Idempotent-Replayed").Should().Equal("true");
        secondResponse.Headers.Location.Should().Be(firstResponse.Headers.Location);

        // The replayed body is byte-for-byte the original one; only the header tells the two apart.
        (await secondResponse.Content.ReadAsStringAsync()).Should().Be(await firstResponse.Content.ReadAsStringAsync());
        var first = await firstResponse.ReadAsAsync<SubmitPurchaseOrderResult>();
        var second = await secondResponse.ReadAsAsync<SubmitPurchaseOrderResult>();
        second.PurchaseOrder.Id.Should().Be(first.PurchaseOrder.Id);
        second.Should().BeEquivalentTo(first);

        var customer = await (await _client.GetAsync($"/api/v1/customers/{customerId}")).ReadAsAsync<CustomerDto>();
        customer.Memberships.Should().ContainSingle().Which.Type.Should().Be(MembershipType.BookClub);

        var next = await _client.SubmitOrderAsync(customerId, Requests.Product(1));
        next.PurchaseOrder.Id.Should().Be(first.PurchaseOrder.Id + 1, "the replay must not have allocated or stored another order");
    }

    [Fact]
    public async Task Reusing_a_key_with_a_different_body_is_a_conflict()
    {
        var customerId = await _client.CreateCustomerAsync();
        var key = NewKey();

        var original = await _client.PostJsonWithIdempotencyKeyAsync(Url, Requests.Order(customerId, null, Requests.Product(1)), key);
        var reused = await _client.PostJsonWithIdempotencyKeyAsync(Url, Requests.Order(customerId, null, Requests.Product(2)), key);

        original.StatusCode.Should().Be(HttpStatusCode.Created);
        reused.StatusCode.Should().Be(HttpStatusCode.Conflict);
        reused.Content.Headers.ContentType!.MediaType.Should().Be(HttpJson.ProblemContentType);
        var problem = await reused.ReadProblemAsync();
        problem.Code().Should().Be("idempotency.payload_mismatch");
        problem.Status.Should().Be((int)HttpStatusCode.Conflict);
    }

    [Fact]
    public Task A_key_longer_than_128_characters_is_a_bad_request() => AssertRejectedKeyAsync(new string('k', 129));

    [Fact]
    public Task A_key_containing_a_space_is_a_bad_request() => AssertRejectedKeyAsync("two words");

    [Fact]
    public Task A_whitespace_only_key_is_a_bad_request() => AssertRejectedKeyAsync("   ");

    [Fact]
    public async Task A_present_but_empty_key_is_a_bad_request_instead_of_silently_disabling_idempotency()
    {
        // HttpClient and the in-memory test host drop a header whose value is empty, so the request is built
        // directly on the HttpContext, exactly as Kestrel presents "Idempotency-Key:" with an empty value.
        var customerId = await _client.CreateCustomerAsync();
        var body = JsonSerializer.SerializeToUtf8Bytes(Requests.Order(customerId, null, Requests.Product(1)), HttpJson.Options);

        var context = await _factory.Server.SendAsync(http =>
        {
            http.Request.Method = HttpMethods.Post;
            http.Request.Path = Url;
            http.Request.ContentType = "application/json";
            http.Request.Body = new MemoryStream(body);
            ((IDictionary<string, StringValues>)http.Request.Headers).Add(PurchaseOrdersController.IdempotencyKeyHeader, new StringValues(string.Empty));
        });

        context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        context.Response.ContentType.Should().StartWith(HttpJson.ProblemContentType);
        using var reader = new StreamReader(context.Response.Body);
        var problem = JsonSerializer.Deserialize<ValidationProblemDetails>(await reader.ReadToEndAsync(), HttpJson.Options);
        problem.Should().NotBeNull();
        problem!.Code().Should().Be("request.invalid");
        problem.Errors.Should().ContainKey(PurchaseOrdersController.IdempotencyKeyHeader);
    }

    [Fact]
    public async Task A_key_sent_in_two_headers_is_a_bad_request_instead_of_being_joined()
    {
        var customerId = await _client.CreateCustomerAsync();
        var body = JsonSerializer.SerializeToUtf8Bytes(Requests.Order(customerId, null, Requests.Product(1)), HttpJson.Options);

        var context = await _factory.Server.SendAsync(http =>
        {
            http.Request.Method = HttpMethods.Post;
            http.Request.Path = Url;
            http.Request.ContentType = "application/json";
            http.Request.Body = new MemoryStream(body);
            http.Request.Headers.Append(PurchaseOrdersController.IdempotencyKeyHeader, "first");
            http.Request.Headers.Append(PurchaseOrdersController.IdempotencyKeyHeader, "second");
        });

        context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        using var reader = new StreamReader(context.Response.Body);
        var problem = JsonSerializer.Deserialize<ValidationProblemDetails>(await reader.ReadToEndAsync(), HttpJson.Options);
        problem!.Errors.Should().ContainKey(PurchaseOrdersController.IdempotencyKeyHeader);
    }

    private async Task AssertRejectedKeyAsync(string key)
    {
        var customerId = await _client.CreateCustomerAsync();

        var response = await _client.PostJsonWithIdempotencyKeyAsync(Url, Requests.Order(customerId, null, Requests.Product(1)), key);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.ReadProblemAsync();
        problem.Errors.Should().ContainKey("Idempotency-Key");
    }

    [Fact]
    public async Task Different_keys_create_different_orders()
    {
        var customerId = await _client.CreateCustomerAsync();
        var body = Requests.Order(customerId, null, Requests.Product(1));

        var first = await (await _client.PostJsonWithIdempotencyKeyAsync(Url, body, NewKey())).ReadAsAsync<SubmitPurchaseOrderResult>();
        var second = await (await _client.PostJsonWithIdempotencyKeyAsync(Url, body, NewKey())).ReadAsAsync<SubmitPurchaseOrderResult>();

        second.PurchaseOrder.Id.Should().NotBe(first.PurchaseOrder.Id);
    }
}
