using System.Net;
using FluentAssertions;
using FunBooksAndVideos.Api.Tests.Infrastructure;
using FunBooksAndVideos.Application.Dtos;
using FunBooksAndVideos.Domain.Catalog;
using FunBooksAndVideos.Infrastructure.Seeding;

namespace FunBooksAndVideos.Api.Tests.Endpoints;

public sealed class ProductsEndpointTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public ProductsEndpointTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task List_returns_the_seeded_catalog_ordered_by_id()
    {
        var products = await (await _client.GetAsync("/api/v1/products")).ReadAsAsync<IReadOnlyList<ProductDto>>();

        products.Take(4).Should().Equal(
            new ProductDto(DemoData.FirstAidVideoId, DemoData.FirstAidVideoName, ProductKind.Video, DemoData.FirstAidVideoPrice, false),
            new ProductDto(DemoData.GirlOnTheTrainBookId, DemoData.GirlOnTheTrainBookName, ProductKind.Book, DemoData.GirlOnTheTrainBookPrice, true),
            new ProductDto(DemoData.CleanCodeBookId, DemoData.CleanCodeBookName, ProductKind.Book, DemoData.CleanCodeBookPrice, true),
            new ProductDto(DemoData.YogaVideoId, DemoData.YogaVideoName, ProductKind.Video, DemoData.YogaVideoPrice, false));
        products.Select(product => product.Id).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task Get_returns_a_product_or_404()
    {
        var found = await _client.GetAsync($"/api/v1/products/{DemoData.GirlOnTheTrainBookId}");
        var missing = await _client.GetAsync("/api/v1/products/987654321");

        found.StatusCode.Should().Be(HttpStatusCode.OK);
        (await found.ReadAsAsync<ProductDto>()).RequiresShipping.Should().BeTrue();
        missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await missing.ReadProblemAsync()).Code().Should().Be("product.not_found");
    }

    [Theory]
    [InlineData("Book", true)]
    [InlineData("Video", false)]
    public async Task Create_returns_201_and_the_product_is_retrievable(string kind, bool requiresShipping)
    {
        var response = await _client.PostJsonAsync("/api/v1/products", Requests.ProductToCreate(kind, $"New {kind}", 12.5m));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var product = await response.ReadAsAsync<ProductDto>();
        product.Kind.ToString().Should().Be(kind);
        product.RequiresShipping.Should().Be(requiresShipping);
        product.Price.Should().Be(12.50m);
        response.Headers.Location!.AbsolutePath.Should().Be($"/api/v1/products/{product.Id}");
        (await (await _client.GetAsync(response.Headers.Location)).ReadAsAsync<ProductDto>()).Should().Be(product);
    }

    [Fact]
    public async Task Create_rejects_unknown_kinds_blank_names_and_negative_prices_as_bad_requests()
    {
        var unknownKind = await _client.PostJsonAsync("/api/v1/products", Requests.ProductToCreate("Magazine", "Wired", 5m));
        var blankName = await _client.PostJsonAsync("/api/v1/products", Requests.ProductToCreate("Book", " ", 5m));
        var negativePrice = await _client.PostJsonAsync("/api/v1/products", Requests.ProductToCreate("Book", "Title", -5m));
        var missingKind = await _client.PostJsonAsync("/api/v1/products", new { name = "Title", price = 5m });

        unknownKind.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await blankName.ReadProblemAsync()).Errors.Should().ContainKey("name");
        (await negativePrice.ReadProblemAsync()).Errors.Should().ContainKey("price");
        (await missingKind.ReadProblemAsync()).Errors.Should().ContainKey("kind");
    }

    [Fact]
    public async Task Create_without_a_price_is_a_bad_request()
    {
        var response = await _client.PostJsonAsync("/api/v1/products", new { kind = "Book", name = "Missing price" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.ReadProblemAsync();
        problem.Code().Should().Be("request.invalid");
        problem.Errors.Should().ContainKey("price");
    }

    [Fact]
    public async Task Create_accepts_an_explicit_zero_price()
    {
        var response = await _client.PostJsonAsync("/api/v1/products", Requests.ProductToCreate("Video", "Free sample", 0m));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await response.ReadAsAsync<ProductDto>()).Price.Should().Be(0.00m);
        (await response.Content.ReadAsStringAsync()).Should().Contain("\"price\":0.00");
    }

    [Fact]
    public async Task Prices_are_normalised_to_two_decimals_in_json()
    {
        var response = await _client.PostAsync(
            "/api/v1/products",
            new StringContent("{\"kind\":\"Video\",\"name\":\"Three zeros\",\"price\":10.000}", System.Text.Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("\"price\":10.00").And.NotContain("\"price\":10.000");
    }

    [Fact]
    public async Task Create_rejects_prices_with_more_than_two_decimals_as_unprocessable()
    {
        var response = await _client.PostJsonAsync("/api/v1/products", Requests.ProductToCreate("Video", "Title", 1.999m));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await response.ReadProblemAsync()).Code().Should().Be("money.precision");
    }
}
