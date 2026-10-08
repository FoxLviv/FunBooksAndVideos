using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using FunBooksAndVideos.Api.Contracts;
using FunBooksAndVideos.Api.Tests.Infrastructure;
using FunBooksAndVideos.Application.Dtos;
using FunBooksAndVideos.Application.PurchaseOrders.SubmitPurchaseOrder;
using FunBooksAndVideos.Infrastructure.Seeding;

namespace FunBooksAndVideos.Api.Tests.Endpoints;

public sealed class PurchaseOrdersEndpointTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public PurchaseOrdersEndpointTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Submit_returns_201_with_a_location_that_resolves_to_the_order()
    {
        var response = await _client.PostJsonAsync("/api/v1/purchase-orders", Requests.Order(DemoData.JaneDoeCustomerId, null, Requests.Product(DemoData.YogaVideoId)));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");

        var order = await (await _client.GetAsync(response.Headers.Location)).ReadAsAsync<PurchaseOrderDto>();
        order.Lines.Should().ContainSingle().Which.ProductId.Should().Be(DemoData.YogaVideoId);
    }

    [Fact]
    public async Task Submit_without_lines_is_a_bad_request()
    {
        var response = await _client.PostJsonAsync("/api/v1/purchase-orders", new { customerId = DemoData.JaneDoeCustomerId, lines = Array.Empty<object>() });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.ReadProblemAsync();
        problem.Code().Should().Be("request.invalid");
        problem.Errors.Should().ContainKey("lines");
    }

    [Fact]
    public async Task Submit_with_more_than_the_maximum_number_of_lines_is_a_bad_request()
    {
        var tooMany = Enumerable.Repeat(Requests.Product(DemoData.YogaVideoId), SubmitPurchaseOrderRequest.MaxLines + 1).ToArray();
        var justEnough = Enumerable.Repeat(Requests.Product(DemoData.YogaVideoId), SubmitPurchaseOrderRequest.MaxLines).ToArray();

        var rejected = await _client.PostJsonAsync("/api/v1/purchase-orders", Requests.Order(DemoData.JaneDoeCustomerId, null, tooMany));
        var accepted = await _client.PostJsonAsync("/api/v1/purchase-orders", Requests.Order(DemoData.JaneDoeCustomerId, null, justEnough));

        rejected.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await rejected.ReadProblemAsync()).Errors.Should().ContainKey("lines");
        accepted.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Unknown_json_members_are_rejected_instead_of_silently_ignored()
    {
        var unknownLineMember = await _client.PostJsonAsync("/api/v1/purchase-orders", Requests.Order(DemoData.JaneDoeCustomerId, null, new { type = "Product", productId = DemoData.YogaVideoId, quantity = 3 }));
        var unknownTopLevelMember = await _client.PostJsonAsync("/api/v1/purchase-orders", new { customerId = DemoData.JaneDoeCustomerId, lines = new[] { Requests.Product(DemoData.YogaVideoId) }, currency = "EUR" });

        unknownLineMember.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var lineProblem = await unknownLineMember.ReadProblemAsync();
        lineProblem.Errors.Should().ContainSingle().Which.Should().Match<KeyValuePair<string, string[]>>(error =>
            error.Key.StartsWith("$.lines[0]", StringComparison.Ordinal) && error.Value.Single().Contains("'quantity'", StringComparison.Ordinal));
        unknownTopLevelMember.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await unknownTopLevelMember.ReadProblemAsync()).Errors.Keys.Should().Contain(key => key.Contains("currency", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Submit_with_a_non_positive_customer_id_is_a_bad_request()
    {
        var response = await _client.PostJsonAsync("/api/v1/purchase-orders", Requests.Order(0, null, Requests.Product(DemoData.YogaVideoId)));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.ReadProblemAsync()).Errors.Should().ContainKey("customerId");
    }

    [Fact]
    public async Task A_product_line_needs_a_product_id_and_a_membership_line_a_membership_type()
    {
        var missingProduct = await _client.PostJsonAsync("/api/v1/purchase-orders", Requests.Order(DemoData.JaneDoeCustomerId, null, new { type = "Product" }));
        var missingMembership = await _client.PostJsonAsync("/api/v1/purchase-orders", Requests.Order(DemoData.JaneDoeCustomerId, null, new { type = "Membership" }));

        missingProduct.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await missingProduct.ReadProblemAsync()).Errors.Should().ContainKey("lines[0].productId");
        missingMembership.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await missingMembership.ReadProblemAsync()).Errors.Should().ContainKey("lines[0].membershipType");
    }

    [Theory]
    [InlineData("{\"type\":\"Product\",\"productId\":4,\"membershipType\":\"BookClub\"}", "membershipType")]
    [InlineData("{\"type\":\"Product\",\"productId\":4,\"membershipType\":null}", "membershipType")]
    [InlineData("{\"type\":\"Membership\",\"membershipType\":\"BookClub\",\"productId\":4}", "productId")]
    [InlineData("{\"type\":\"Membership\",\"membershipType\":\"BookClub\",\"productId\":null}", "productId")]
    public async Task A_line_cannot_carry_the_property_of_the_other_branch_not_even_as_null(string line, string offendingProperty)
    {
        var body = $"{{\"customerId\":{DemoData.JaneDoeCustomerId},\"lines\":[{line}]}}";

        var response = await _client.PostAsync("/api/v1/purchase-orders", new StringContent(body, System.Text.Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.ReadProblemAsync();
        problem.Code().Should().Be("request.invalid");
        problem.Errors.Should().ContainSingle().Which.Should().Match<KeyValuePair<string, string[]>>(error =>
            error.Key.StartsWith("$.lines[0]", StringComparison.Ordinal) && error.Value.Single().Contains(offendingProperty, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("{\"type\":\"Product\",\"productId\":\"4\"}")]
    [InlineData("{\"type\":\"Product\",\"productId\":4.5}")]
    [InlineData("[\"not an object\"]")]
    public async Task Malformed_line_shapes_are_bad_requests(string line)
    {
        var body = $"{{\"customerId\":{DemoData.JaneDoeCustomerId},\"lines\":[{line}]}}";

        var response = await _client.PostAsync("/api/v1/purchase-orders", new StringContent(body, System.Text.Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.ReadProblemAsync()).Code().Should().Be("request.invalid");
    }

    [Theory]
    [InlineData("{\"customerId\":4567890,\"lines\":[{\"type\":\"Subscription\"}]}", "type")]
    [InlineData("{\"customerId\":4567890,\"lines\":[{\"type\":\"Membership\",\"membershipType\":\"GoldClub\"}]}", "membershipType")]
    [InlineData("{\"customerId\":4567890,\"lines\":[{\"type\":1,\"productId\":1}]}", "type")]
    public async Task Unknown_enum_values_are_rejected_with_the_json_path(string body, string offendingProperty)
    {
        var response = await _client.PostAsync("/api/v1/purchase-orders", new StringContent(body, System.Text.Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.ReadProblemAsync();
        problem.Code().Should().Be("request.invalid");
        problem.Errors.Should().ContainSingle().Which.Should().Match<KeyValuePair<string, string[]>>(error =>
            error.Key.StartsWith("$.lines[0]", StringComparison.Ordinal) && error.Value.Single().Contains(offendingProperty, StringComparison.Ordinal));
        problem.Errors.Should().NotContainKey("request");
    }

    [Theory]
    [InlineData("{\"customerId\":4567890,\"lines\":[null]}", "lines[0]")]
    [InlineData("{\"customerId\":4567890,\"lines\":[{\"type\":\"Product\",\"productId\":1},null]}", "lines[1]")]
    public async Task Null_item_lines_are_a_bad_request_keyed_by_position(string body, string expectedErrorKey)
    {
        var response = await _client.PostAsync("/api/v1/purchase-orders", new StringContent(body, System.Text.Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.ReadProblemAsync();
        problem.Code().Should().Be("request.invalid");
        problem.Errors.Should().ContainKey(expectedErrorKey).WhoseValue.Single().Should().Be("Item line must not be null.");
    }

    [Theory]
    [InlineData("BookClub, VideoClub")]
    [InlineData("BookClub,VideoClub")]
    [InlineData("3")]
    [InlineData("1")]
    public async Task Composite_and_numeric_membership_types_are_rejected(string membershipType)
    {
        var body = "{\"customerId\":4567890,\"lines\":[{\"type\":\"Membership\",\"membershipType\":\"" + membershipType + "\"}]}";

        var response = await _client.PostAsync("/api/v1/purchase-orders", new StringContent(body, System.Text.Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.ReadProblemAsync();
        problem.Code().Should().Be("request.invalid");
        problem.Errors.Should().ContainSingle().Which.Should().Match<KeyValuePair<string, string[]>>(error =>
            error.Key.StartsWith("$.lines[0]", StringComparison.Ordinal)
            && error.Value.Single().Contains("membershipType", StringComparison.Ordinal)
            && error.Value.Single().Contains(membershipType, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Enum_names_are_matched_case_insensitively()
    {
        var customerId = await _client.CreateCustomerAsync("Case Insensitive");
        const string Template = "{\"customerId\":{0},\"lines\":[{\"type\":\"membership\",\"membershipType\":\"bookclub\"}]}";
        var body = Template.Replace("{0}", customerId.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);

        var response = await _client.PostAsync("/api/v1/purchase-orders", new StringContent(body, System.Text.Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.ReadAsAsync<SubmitPurchaseOrderResult>();
        result.PurchaseOrder.Total.Should().Be(DemoData.BookClubPrice);
        (await response.Content.ReadAsStringAsync()).Should().Contain("\"BookClub\"").And.NotContain("\"Premium\"");
    }

    [Fact]
    public async Task Malformed_json_and_wrong_content_type_are_rejected()
    {
        var malformed = await _client.PostAsync("/api/v1/purchase-orders", new StringContent("{ not json", System.Text.Encoding.UTF8, "application/json"));
        var wrongType = await _client.PostAsync("/api/v1/purchase-orders", new StringContent("customerId=1", System.Text.Encoding.UTF8, "text/plain"));

        malformed.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        wrongType.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
    }

    [Fact]
    public async Task A_negative_expected_total_is_a_bad_request()
    {
        var response = await _client.PostJsonAsync("/api/v1/purchase-orders", Requests.Order(DemoData.JaneDoeCustomerId, -1m, Requests.Product(DemoData.YogaVideoId)));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.ReadProblemAsync()).Errors.Should().ContainKey("expectedTotal");
    }

    [Fact]
    public async Task An_expected_total_with_three_decimals_is_unprocessable()
    {
        var response = await _client.PostJsonAsync("/api/v1/purchase-orders", Requests.Order(DemoData.JaneDoeCustomerId, 9.991m, Requests.Product(DemoData.YogaVideoId)));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await response.ReadProblemAsync()).Errors.Should().ContainKey("expectedTotal");
    }

    [Theory]
    [InlineData("/api/v1/purchase-orders/0")]
    [InlineData("/api/v1/purchase-orders/-1")]
    [InlineData("/api/v1/purchase-orders/abc")]
    public async Task Invalid_order_ids_do_not_match_the_route(string url)
    {
        var response = await _client.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Unknown_orders_are_reported_with_a_problem_code()
    {
        var order = await _client.GetAsync("/api/v1/purchase-orders/999999999");
        var slip = await _client.GetAsync("/api/v1/purchase-orders/999999999/shipping-slip");

        order.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await order.ReadProblemAsync()).Code().Should().Be("purchase_order.not_found");
        slip.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await slip.ReadProblemAsync()).Code().Should().Be("purchase_order.not_found");
    }

    [Fact]
    public async Task Enums_are_serialised_as_names_and_money_with_two_decimals()
    {
        var response = await _client.PostJsonAsync("/api/v1/purchase-orders", Requests.Order(DemoData.JaneDoeCustomerId, null, Requests.Product(DemoData.CleanCodeBookId)));
        var json = await response.Content.ReadAsStringAsync();

        json.Should().Contain("\"status\":\"Processed\"");
        json.Should().Contain("\"productKind\":\"Book\"");
        json.Should().Contain("\"total\":29.99");
        json.Should().NotContain("\"status\":2");
    }
}
