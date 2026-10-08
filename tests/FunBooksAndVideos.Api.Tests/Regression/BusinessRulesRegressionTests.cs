using System.Net;
using FluentAssertions;
using FunBooksAndVideos.Api.Tests.Infrastructure;
using FunBooksAndVideos.Application.Dtos;
using FunBooksAndVideos.Application.PurchaseOrders.SubmitPurchaseOrder;
using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Infrastructure.Seeding;

namespace FunBooksAndVideos.Api.Tests.Regression;

/// <summary>Behavioural contract of the purchase order processor, exercised through the HTTP API.</summary>
[Trait("Category", "Regression")]
public sealed class BusinessRulesRegressionTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public BusinessRulesRegressionTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Digital_only_orders_produce_no_shipping_slip_and_apply_no_rule()
    {
        var customerId = await _client.CreateCustomerAsync();

        var result = await _client.SubmitOrderAsync(customerId, Requests.Product(DemoData.FirstAidVideoId), Requests.Product(DemoData.YogaVideoId));

        result.ShippingSlip.Should().BeNull();
        result.MembershipActivations.Should().BeEmpty();
        result.AppliedRules.Should().BeEmpty();
        result.PurchaseOrder.Total.Should().Be(DemoData.FirstAidVideoPrice + DemoData.YogaVideoPrice);

        var slipResponse = await _client.GetAsync($"/api/v1/purchase-orders/{result.PurchaseOrder.Id}/shipping-slip");
        slipResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await slipResponse.ReadProblemAsync()).Code().Should().Be("shipping_slip.not_found");
    }

    [Fact]
    public async Task Membership_only_orders_activate_the_membership_and_ship_nothing()
    {
        var customerId = await _client.CreateCustomerAsync(withAddress: false);

        var result = await _client.SubmitOrderAsync(customerId, Requests.Membership("VideoClub"));

        result.AppliedRules.Should().Equal("BR1.MembershipActivation");
        result.ShippingSlip.Should().BeNull();
        result.MembershipActivations.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            MembershipType = MembershipType.VideoClub,
            Outcome = MembershipActivationOutcome.Activated,
        });

        var customer = await (await _client.GetAsync($"/api/v1/customers/{customerId}")).ReadAsAsync<CustomerDto>();
        customer.ActiveClubs.Should().Equal(Club.Video);
    }

    [Fact]
    public async Task Buying_the_same_membership_again_is_idempotent()
    {
        var customerId = await _client.CreateCustomerAsync();
        await _client.SubmitOrderAsync(customerId, Requests.Membership("BookClub"));

        var second = await _client.SubmitOrderAsync(customerId, Requests.Membership("BookClub"));

        second.MembershipActivations.Should().ContainSingle().Which.Outcome.Should().Be(MembershipActivationOutcome.AlreadyActive);
        second.MembershipActivations.Single().ActivatedAt.Should().BeNull();
        var customer = await (await _client.GetAsync($"/api/v1/customers/{customerId}")).ReadAsAsync<CustomerDto>();
        customer.Memberships.Should().HaveCount(1);
    }

    [Fact]
    public async Task Premium_upgrades_a_book_club_member_and_covers_later_single_club_requests()
    {
        var customerId = await _client.CreateCustomerAsync();
        await _client.SubmitOrderAsync(customerId, Requests.Membership("BookClub"));

        var upgrade = await _client.SubmitOrderAsync(customerId, Requests.Membership("Premium"));
        var redundant = await _client.SubmitOrderAsync(customerId, Requests.Membership("VideoClub"));

        upgrade.MembershipActivations.Single().Outcome.Should().Be(MembershipActivationOutcome.Activated);
        redundant.MembershipActivations.Single().Outcome.Should().Be(MembershipActivationOutcome.AlreadyActive);
        var customer = await (await _client.GetAsync($"/api/v1/customers/{customerId}")).ReadAsAsync<CustomerDto>();
        customer.Memberships.Select(membership => membership.Type).Should().Equal(MembershipType.BookClub, MembershipType.Premium);
        customer.ActiveClubs.Should().Equal(Club.Book, Club.Video);
    }

    [Fact]
    public async Task Book_club_and_video_club_can_be_bought_together()
    {
        var customerId = await _client.CreateCustomerAsync();

        var result = await _client.SubmitOrderAsync(customerId, Requests.Membership("BookClub"), Requests.Membership("VideoClub"));

        result.MembershipActivations.Select(activation => activation.Outcome).Should().OnlyContain(outcome => outcome == MembershipActivationOutcome.Activated);
        result.PurchaseOrder.Total.Should().Be(DemoData.BookClubPrice + DemoData.VideoClubPrice);
    }

    [Fact]
    public async Task Repeated_physical_products_are_packed_as_one_item_with_a_quantity()
    {
        var customerId = await _client.CreateCustomerAsync();

        var result = await _client.SubmitOrderAsync(
            customerId,
            Requests.Product(DemoData.GirlOnTheTrainBookId),
            Requests.Product(DemoData.CleanCodeBookId),
            Requests.Product(DemoData.GirlOnTheTrainBookId),
            Requests.Product(DemoData.FirstAidVideoId));

        result.PurchaseOrder.Lines.Should().HaveCount(4);
        result.ShippingSlip.Should().NotBeNull();
        result.ShippingSlip!.Items.Should().BeEquivalentTo(
        [
            new ShippingSlipItemDto(DemoData.GirlOnTheTrainBookId, DemoData.GirlOnTheTrainBookName, 2),
            new ShippingSlipItemDto(DemoData.CleanCodeBookId, DemoData.CleanCodeBookName, 1),
        ]);
        result.ShippingSlip.Address.Line1.Should().Be("12 St James's Square");
    }

    [Fact]
    public async Task Physical_products_for_a_customer_without_address_are_rejected_and_nothing_is_persisted()
    {
        var customerId = await _client.CreateCustomerAsync(withAddress: false);

        var response = await _client.PostJsonAsync("/api/v1/purchase-orders", Requests.Order(
            customerId,
            null,
            Requests.Membership("BookClub"),
            Requests.Product(DemoData.GirlOnTheTrainBookId)));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var problem = await response.ReadProblemAsync();
        problem.Code().Should().Be("customer.shipping_address.missing");

        // BR1 ran before BR2 failed, yet the membership was not persisted: the unit of work was never committed.
        var customer = await (await _client.GetAsync($"/api/v1/customers/{customerId}")).ReadAsAsync<CustomerDto>();
        customer.Memberships.Should().BeEmpty();

        // The seeded digital-only customer behaves the same way.
        var seeded = await _client.PostJsonAsync("/api/v1/purchase-orders", Requests.Order(DemoData.DigitalOnlyCustomerId, null, Requests.Product(DemoData.CleanCodeBookId)));
        seeded.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Overlapping_memberships_in_one_order_are_rejected()
    {
        var customerId = await _client.CreateCustomerAsync();

        var response = await _client.PostJsonAsync("/api/v1/purchase-orders", Requests.Order(customerId, null, Requests.Membership("Premium"), Requests.Membership("BookClub")));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var problem = await response.ReadProblemAsync();
        problem.Code().Should().Be("purchase_order.memberships.overlap");
        problem.Detail.Should().Contain("Premium").And.Contain("BookClub");
    }

    [Fact]
    public async Task A_stale_client_total_is_rejected()
    {
        var customerId = await _client.CreateCustomerAsync();

        var response = await _client.PostJsonAsync("/api/v1/purchase-orders", Requests.Order(customerId, 10.00m, Requests.Product(DemoData.FirstAidVideoId)));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var problem = await response.ReadProblemAsync();
        problem.Code().Should().Be("validation.failed");
        problem.Errors.Should().ContainKey("expectedTotal").WhoseValue.Single().Should().Contain("10.00").And.Contain("19.50");
    }

    [Fact]
    public async Task Unknown_products_and_customers_are_reported_per_field()
    {
        var customerId = await _client.CreateCustomerAsync();

        var unknownProducts = await _client.PostJsonAsync("/api/v1/purchase-orders", Requests.Order(customerId, null, Requests.Product(DemoData.FirstAidVideoId), Requests.Product(999_999)));
        var unknownCustomer = await _client.PostJsonAsync("/api/v1/purchase-orders", Requests.Order(999_999_999, null, Requests.Product(DemoData.FirstAidVideoId)));

        unknownProducts.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await unknownProducts.ReadProblemAsync()).Errors.Should().ContainKey("lines[1].productId");
        unknownCustomer.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await unknownCustomer.ReadProblemAsync()).Errors.Should().ContainKey("customerId");
    }

    [Fact]
    public async Task A_product_added_through_the_api_can_be_ordered_right_away()
    {
        var customerId = await _client.CreateCustomerAsync();
        var created = await _client.PostJsonAsync("/api/v1/products", Requests.ProductToCreate("Book", "The Pragmatic Programmer", 42.00m));
        var product = await created.ReadAsAsync<ProductDto>();

        var result = await _client.SubmitOrderAsync(customerId, Requests.Product(product.Id));

        result.PurchaseOrder.Total.Should().Be(42.00m);
        result.ShippingSlip!.Items.Should().ContainSingle().Which.ProductName.Should().Be("The Pragmatic Programmer");
    }

    [Fact]
    public async Task Each_submitted_order_gets_its_own_increasing_identifier()
    {
        var customerId = await _client.CreateCustomerAsync();

        var first = await _client.SubmitOrderAsync(customerId, Requests.Product(DemoData.FirstAidVideoId));
        var second = await _client.SubmitOrderAsync(customerId, Requests.Product(DemoData.FirstAidVideoId));

        second.PurchaseOrder.Id.Should().BeGreaterThan(first.PurchaseOrder.Id);
        first.PurchaseOrder.Id.Should().BeGreaterThanOrEqualTo(DemoData.FirstPurchaseOrderId);
    }
}
