using System.Net;
using FluentAssertions;
using FunBooksAndVideos.Api.Tests.Infrastructure;
using FunBooksAndVideos.Application.Dtos;
using FunBooksAndVideos.Infrastructure.Seeding;

namespace FunBooksAndVideos.Api.Tests.Endpoints;

public sealed class CustomersEndpointTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public CustomersEndpointTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_returns_201_with_the_customer_and_a_location()
    {
        var response = await _client.PostJsonAsync("/api/v1/customers", Requests.Customer("  Grace Hopper "));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var customer = await response.ReadAsAsync<CustomerDto>();
        customer.Id.Should().BeGreaterThan(DemoData.DigitalOnlyCustomerId);
        customer.Name.Should().Be("Grace Hopper");
        customer.ShippingAddress.Should().Be(new ShippingAddressDto("12 St James's Square", null, "London", "SW1Y 4JH", "United Kingdom"));
        customer.Memberships.Should().BeEmpty();
        customer.ActiveClubs.Should().BeEmpty();
        response.Headers.Location!.AbsolutePath.Should().Be($"/api/v1/customers/{customer.Id}");

        var fetched = await (await _client.GetAsync(response.Headers.Location)).ReadAsAsync<CustomerDto>();
        fetched.Should().BeEquivalentTo(customer);
    }

    [Fact]
    public async Task Create_without_address_is_allowed()
    {
        var response = await _client.PostJsonAsync("/api/v1/customers", Requests.Customer("Dan", withAddress: false));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await response.ReadAsAsync<CustomerDto>()).ShippingAddress.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_requires_a_name(string name)
    {
        var response = await _client.PostJsonAsync("/api/v1/customers", new { name });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.ReadProblemAsync()).Errors.Should().ContainKey("name");
    }

    [Fact]
    public async Task Create_validates_the_address_fields()
    {
        var response = await _client.PostJsonAsync("/api/v1/customers", new { name = "Ada", shippingAddress = new { line1 = "Street", postalCode = "1", country = "UK" } });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.ReadProblemAsync()).Errors.Should().ContainKey("shippingAddress.city");
    }

    [Fact]
    public async Task Create_rejects_names_that_exceed_the_limit()
    {
        var response = await _client.PostJsonAsync("/api/v1/customers", new { name = new string('x', 201) });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.ReadProblemAsync()).Errors.Should().ContainKey("name");
    }

    [Fact]
    public async Task Get_returns_the_seeded_kata_customer()
    {
        var customer = await (await _client.GetAsync($"/api/v1/customers/{DemoData.JaneDoeCustomerId}")).ReadAsAsync<CustomerDto>();

        customer.Name.Should().Be(DemoData.JaneDoeName);
        customer.ShippingAddress!.City.Should().Be("London");
    }

    [Fact]
    public async Task Get_reports_unknown_customers()
    {
        var response = await _client.GetAsync("/api/v1/customers/123456789");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.ReadProblemAsync();
        problem.Code().Should().Be("customer.not_found");
        problem.Status.Should().Be(404);
    }
}
