using System.Net;
using FluentAssertions;
using FunBooksAndVideos.Api.Tests.Infrastructure;
using FunBooksAndVideos.Application.Dtos;
using FunBooksAndVideos.Application.PurchaseOrders.SubmitPurchaseOrder;
using FunBooksAndVideos.Domain.Catalog;
using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Domain.Orders;
using FunBooksAndVideos.Infrastructure.Seeding;
using FunBooksAndVideos.TestKit;

namespace FunBooksAndVideos.Api.Tests.Regression;

/// <summary>
/// Pins the exact scenario of the kata end to end: purchase order 3344656 for customer 4567890 with
/// a video, a book and a book club membership, total 48.50, and both business rules applied.
/// </summary>
[Trait("Category", "Regression")]
public sealed class KataExampleRegressionTests
{
    [Fact]
    public async Task The_kata_purchase_order_is_processed_with_both_business_rules()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostJsonAsync("/api/v1/purchase-orders", Requests.Order(
            DemoData.JaneDoeCustomerId,
            expectedTotal: 48.50m,
            Requests.Product(DemoData.FirstAidVideoId),
            Requests.Product(DemoData.GirlOnTheTrainBookId),
            Requests.Membership("BookClub")));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.AbsolutePath.Should().Be("/api/v1/purchase-orders/3344656");

        var result = await response.ReadAsAsync<SubmitPurchaseOrderResult>();

        result.PurchaseOrder.Should().BeEquivalentTo(new
        {
            Id = 3344656L,
            CustomerId = 4567890L,
            Total = 48.50m,
            Status = PurchaseOrderStatus.Processed,
            CreatedAt = TestClock.Now,
            ProcessedAt = TestClock.Now,
        });
        result.PurchaseOrder.Lines.Should().Equal(
            new OrderLineDto(OrderLineType.Product, "Video \"Comprehensive First Aid Training\"", 19.50m, 1, ProductKind.Video, false, null),
            new OrderLineDto(OrderLineType.Product, "Book \"The Girl on the train\"", 14.00m, 2, ProductKind.Book, true, null),
            new OrderLineDto(OrderLineType.Membership, "Book Club Membership", 15.00m, null, null, null, MembershipType.BookClub));

        // BR1: the membership was activated in the customer account immediately.
        result.MembershipActivations.Should().Equal(new MembershipActivationDto(MembershipType.BookClub, MembershipActivationOutcome.Activated, TestClock.Now));

        // BR2: a shipping slip was generated for the physical product only.
        result.ShippingSlip.Should().NotBeNull();
        result.ShippingSlip!.PurchaseOrderId.Should().Be(3344656);
        result.ShippingSlip.CustomerId.Should().Be(4567890);
        result.ShippingSlip.Address.Should().Be(new ShippingAddressDto("221B Baker Street", null, "London", "NW1 6XE", "United Kingdom"));
        result.ShippingSlip.Items.Should().Equal(new ShippingSlipItemDto(2, "The Girl on the train", 1));
        result.ShippingSlip.GeneratedAt.Should().Be(TestClock.Now);

        result.AppliedRules.Should().Equal("BR1.MembershipActivation", "BR2.ShippingSlipGeneration");

        // Everything is persisted and reachable through the resource endpoints.
        var storedOrder = await (await client.GetAsync(response.Headers.Location)).ReadAsAsync<PurchaseOrderDto>();
        storedOrder.Should().BeEquivalentTo(result.PurchaseOrder);

        var storedSlip = await (await client.GetAsync("/api/v1/purchase-orders/3344656/shipping-slip")).ReadAsAsync<ShippingSlipDto>();
        storedSlip.Should().BeEquivalentTo(result.ShippingSlip);

        var customer = await (await client.GetAsync("/api/v1/customers/4567890")).ReadAsAsync<CustomerDto>();
        customer.Memberships.Should().Equal(new MembershipDto(MembershipType.BookClub, TestClock.Now));
        customer.ActiveClubs.Should().Equal(Club.Book);
    }

    [Fact]
    public async Task The_kata_purchase_order_without_the_optional_expected_total_is_also_accepted()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var result = await client.SubmitOrderAsync(
            DemoData.JaneDoeCustomerId,
            Requests.Product(DemoData.FirstAidVideoId),
            Requests.Product(DemoData.GirlOnTheTrainBookId),
            Requests.Membership("BookClub"));

        result.PurchaseOrder.Id.Should().Be(DemoData.FirstPurchaseOrderId);
        result.PurchaseOrder.Total.Should().Be(DemoData.KataExampleTotal);
    }
}
