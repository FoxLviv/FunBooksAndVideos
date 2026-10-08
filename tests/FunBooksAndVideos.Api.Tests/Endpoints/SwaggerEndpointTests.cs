using System.Net;
using System.Text.Json;
using FluentAssertions;
using FunBooksAndVideos.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FunBooksAndVideos.Api.Tests.Endpoints;

public sealed class SwaggerEndpointTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public SwaggerEndpointTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task The_openapi_document_describes_every_endpoint()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = document.RootElement.GetProperty("paths").EnumerateObject().Select(path => path.Name).ToList();

        paths.Should().BeEquivalentTo(
        [
            "/api/v1/purchase-orders",
            "/api/v1/purchase-orders/{id}",
            "/api/v1/purchase-orders/{id}/shipping-slip",
            "/api/v1/customers",
            "/api/v1/customers/{id}",
            "/api/v1/products",
            "/api/v1/products/{id}",
            "/api/v1/membership-plans",
        ]);
        document.RootElement.GetProperty("info").GetProperty("title").GetString().Should().Be("FunBooksAndVideos API");

        var submit = document.RootElement.GetProperty("paths").GetProperty("/api/v1/purchase-orders").GetProperty("post");
        submit.GetProperty("summary").GetString().Should().Contain("Submits a purchase order");
        submit.GetProperty("responses").EnumerateObject().Select(response => response.Name).Should().Contain(["201", "400", "409", "422"]);
    }

    [Fact]
    public async Task Membership_types_are_documented_as_strings()
    {
        using var client = _factory.CreateClient();

        var json = await client.GetStringAsync("/swagger/v1/swagger.json");

        json.Should().Contain("\"BookClub\"").And.Contain("\"VideoClub\"").And.Contain("\"Premium\"");
    }

    [Fact]
    public async Task Order_lines_are_documented_as_a_one_of_discriminated_by_type()
    {
        using var document = await GetDocumentAsync();
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");
        var line = schemas.GetProperty("OrderLineRequestModel");

        line.GetProperty("oneOf").EnumerateArray().Select(option => option.GetProperty("$ref").GetString()).Should().Equal(
            "#/components/schemas/ProductLineRequest",
            "#/components/schemas/MembershipLineRequest");
        line.TryGetProperty("properties", out _).Should().BeFalse();

        var discriminator = line.GetProperty("discriminator");
        discriminator.GetProperty("propertyName").GetString().Should().Be("type");
        var mapping = discriminator.GetProperty("mapping");
        mapping.GetProperty("Product").GetString().Should().Be("#/components/schemas/ProductLineRequest");
        mapping.GetProperty("Membership").GetString().Should().Be("#/components/schemas/MembershipLineRequest");
    }

    [Fact]
    public async Task Product_and_membership_line_schemas_declare_their_required_properties()
    {
        using var document = await GetDocumentAsync();
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");
        var product = schemas.GetProperty("ProductLineRequest");
        var membership = schemas.GetProperty("MembershipLineRequest");

        Required(product).Should().BeEquivalentTo(["type", "productId"]);
        product.GetProperty("additionalProperties").GetBoolean().Should().BeFalse();
        product.GetProperty("properties").GetProperty("type").GetProperty("enum").EnumerateArray().Select(value => value.GetString()).Should().Equal("Product");
        var productId = product.GetProperty("properties").GetProperty("productId");
        productId.GetProperty("type").GetString().Should().Be("integer");
        productId.GetProperty("format").GetString().Should().Be("int64");
        productId.GetProperty("minimum").GetInt64().Should().Be(1);

        Required(membership).Should().BeEquivalentTo(["type", "membershipType"]);
        membership.GetProperty("additionalProperties").GetBoolean().Should().BeFalse();
        membership.GetProperty("properties").GetProperty("type").GetProperty("enum").EnumerateArray().Select(value => value.GetString()).Should().Equal("Membership");
        membership.GetProperty("properties").GetProperty("membershipType").GetProperty("enum").EnumerateArray().Select(value => value.GetString())
            .Should().Equal("BookClub", "VideoClub", "Premium");
    }

    [Fact]
    public async Task Submit_request_lines_reference_the_order_line_schema()
    {
        using var document = await GetDocumentAsync();
        var lines = document.RootElement.GetProperty("components").GetProperty("schemas")
            .GetProperty("SubmitPurchaseOrderRequest").GetProperty("properties").GetProperty("lines");

        lines.GetProperty("type").GetString().Should().Be("array");
        lines.GetProperty("items").GetProperty("$ref").GetString().Should().Be("#/components/schemas/OrderLineRequestModel");
    }

    [Fact]
    public async Task Submit_request_declares_customer_id_and_lines_as_required()
    {
        using var document = await GetDocumentAsync();
        var request = document.RootElement.GetProperty("components").GetProperty("schemas").GetProperty("SubmitPurchaseOrderRequest");

        Required(request).Should().BeEquivalentTo(["customerId", "lines"]);
    }

    [Fact]
    public async Task Swagger_ui_is_served_and_the_root_redirects_to_it()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var ui = await client.GetAsync("/swagger/index.html");
        var root = await client.GetAsync("/");

        ui.StatusCode.Should().Be(HttpStatusCode.OK);
        ui.Content.Headers.ContentType!.MediaType.Should().Be("text/html");
        root.StatusCode.Should().Be(HttpStatusCode.Found);
        root.Headers.Location!.ToString().Should().Be("/swagger");
    }

    [Fact]
    public async Task Error_responses_are_documented_as_problem_json_and_successes_as_json()
    {
        using var document = await GetDocumentAsync();
        var paths = document.RootElement.GetProperty("paths");

        var submit = paths.GetProperty("/api/v1/purchase-orders").GetProperty("post").GetProperty("responses");
        ContentTypes(submit, "201").Should().Equal("application/json");
        ContentTypes(submit, "400").Should().Equal("application/problem+json");
        ContentTypes(submit, "409").Should().Equal("application/problem+json");
        ContentTypes(submit, "422").Should().Equal("application/problem+json");
        ContentTypes(submit, "500").Should().Equal("application/problem+json");

        var getOrder = paths.GetProperty("/api/v1/purchase-orders/{id}").GetProperty("get").GetProperty("responses");
        ContentTypes(getOrder, "200").Should().Equal("application/json");
        ContentTypes(getOrder, "404").Should().Equal("application/problem+json");
    }

    private static List<string> ContentTypes(JsonElement responses, string statusCode) =>
        responses.GetProperty(statusCode).GetProperty("content").EnumerateObject().Select(content => content.Name).ToList();

    private static List<string?> Required(JsonElement schema) =>
        schema.GetProperty("required").EnumerateArray().Select(name => name.GetString()).ToList();

    private async Task<JsonDocument> GetDocumentAsync()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }
}
