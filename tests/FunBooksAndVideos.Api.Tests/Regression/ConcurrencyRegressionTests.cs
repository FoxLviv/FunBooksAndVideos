using System.Net;
using FluentAssertions;
using FunBooksAndVideos.Api.Tests.Infrastructure;
using FunBooksAndVideos.Application.Dtos;
using FunBooksAndVideos.Application.PurchaseOrders.SubmitPurchaseOrder;
using FunBooksAndVideos.Domain.Customers;

namespace FunBooksAndVideos.Api.Tests.Regression;

/// <summary>
/// Concurrent orders for the same customer must neither lose a membership nor activate one twice.
/// Optimistic concurrency detects the conflicts; the retry decorator resolves them.
/// </summary>
[Trait("Category", "Regression")]
public sealed class ConcurrencyRegressionTests : IClassFixture<ApiFactory>
{
    private const int ParallelOrders = 24;

    private readonly HttpClient _client;

    public ConcurrencyRegressionTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Parallel_membership_orders_for_one_customer_activate_each_club_exactly_once()
    {
        var customerId = await _client.CreateCustomerAsync();

        var responses = await Task.WhenAll(Enumerable.Range(0, ParallelOrders).Select(index =>
            _client.PostJsonAsync(
                "/api/v1/purchase-orders",
                Requests.Order(customerId, null, Requests.Membership(index % 2 == 0 ? "BookClub" : "VideoClub")))));

        responses.Should().OnlyContain(response => response.StatusCode == HttpStatusCode.Created);

        var results = await Task.WhenAll(responses.Select(response => response.ReadAsAsync<SubmitPurchaseOrderResult>()));
        var activations = results.SelectMany(result => result.MembershipActivations).ToList();

        activations.Should().HaveCount(ParallelOrders);
        activations.Where(activation => activation.Outcome == MembershipActivationOutcome.Activated)
            .Select(activation => activation.MembershipType)
            .Should().BeEquivalentTo([MembershipType.BookClub, MembershipType.VideoClub]);
        results.Select(result => result.PurchaseOrder.Id).Should().OnlyHaveUniqueItems();

        var customer = await (await _client.GetAsync($"/api/v1/customers/{customerId}")).ReadAsAsync<CustomerDto>();
        customer.Memberships.Select(membership => membership.Type).Should().BeEquivalentTo([MembershipType.BookClub, MembershipType.VideoClub]);
        customer.ActiveClubs.Should().Equal(Club.Book, Club.Video);
    }
}
