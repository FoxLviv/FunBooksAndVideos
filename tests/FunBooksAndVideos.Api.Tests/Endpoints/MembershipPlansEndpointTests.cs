using FluentAssertions;
using FunBooksAndVideos.Api.Tests.Infrastructure;
using FunBooksAndVideos.Application.Dtos;
using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Infrastructure.Seeding;

namespace FunBooksAndVideos.Api.Tests.Endpoints;

public sealed class MembershipPlansEndpointTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public MembershipPlansEndpointTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task List_returns_the_three_plans_with_prices_and_clubs()
    {
        var plans = await (await _client.GetAsync("/api/v1/membership-plans")).ReadAsAsync<IReadOnlyList<MembershipPlanDto>>();

        plans.Should().BeEquivalentTo(
        [
            new MembershipPlanDto(MembershipType.BookClub, "Book Club Membership", DemoData.BookClubPrice, [Club.Book]),
            new MembershipPlanDto(MembershipType.VideoClub, "Video Club Membership", DemoData.VideoClubPrice, [Club.Video]),
            new MembershipPlanDto(MembershipType.Premium, "Premium Membership", DemoData.PremiumPrice, [Club.Book, Club.Video]),
        ],
        options => options.WithStrictOrdering());
    }
}
