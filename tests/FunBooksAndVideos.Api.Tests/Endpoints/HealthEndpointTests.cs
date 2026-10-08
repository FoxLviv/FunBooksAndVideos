using System.Net;
using FluentAssertions;
using FunBooksAndVideos.Api.Tests.Infrastructure;

namespace FunBooksAndVideos.Api.Tests.Endpoints;

public sealed class HealthEndpointTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task The_liveness_probe_reports_healthy()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Be("Healthy");
    }

    [Fact]
    public async Task The_probe_is_not_part_of_the_api_document()
    {
        var document = await _client.GetStringAsync("/swagger/v1/swagger.json");

        document.Should().NotContain("\"/health\"");
    }
}
