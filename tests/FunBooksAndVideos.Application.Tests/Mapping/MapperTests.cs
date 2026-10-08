using FluentAssertions;
using FunBooksAndVideos.Application.Dtos;
using FunBooksAndVideos.Application.Mapping;
using FunBooksAndVideos.Domain.Catalog;
using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Domain.Orders;
using FunBooksAndVideos.Domain.Shipping;
using FunBooksAndVideos.TestKit;

namespace FunBooksAndVideos.Application.Tests.Mapping;

public sealed class MapperTests
{
    [Fact]
    public void Purchase_order_lines_are_mapped_through_the_visitor()
    {
        var order = new PurchaseOrderBuilder().WithKataExampleLines().Build();

        var dto = PurchaseOrderMapper.ToDto(order);

        dto.Should().BeEquivalentTo(new
        {
            Id = 3344656L,
            CustomerId = 4567890L,
            Total = 48.50m,
            Status = PurchaseOrderStatus.Pending,
            CreatedAt = TestClock.Now,
            ProcessedAt = (DateTimeOffset?)null,
        });
        dto.Lines.Should().Equal(
            new OrderLineDto(OrderLineType.Product, "Video \"Comprehensive First Aid Training\"", 19.50m, 1, ProductKind.Video, false, null),
            new OrderLineDto(OrderLineType.Product, "Book \"The Girl on the train\"", 14.00m, 2, ProductKind.Book, true, null),
            new OrderLineDto(OrderLineType.Membership, "Book Club Membership", 15.00m, null, null, null, MembershipType.BookClub));
    }

    [Fact]
    public void A_single_line_can_be_mapped()
    {
        var line = MembershipLine.For(TestPlans.VideoClub());

        PurchaseOrderMapper.ToDto(line).Should().Be(new OrderLineDto(OrderLineType.Membership, "Video Club Membership", 20.00m, null, null, null, MembershipType.VideoClub));
    }

    [Fact]
    public void Customer_mapping_includes_address_memberships_and_active_clubs()
    {
        var customer = new CustomerBuilder().WithMembership(MembershipType.BookClub, TestClock.Now).Build();

        var dto = CustomerMapper.ToDto(customer);

        dto.Id.Should().Be(4567890);
        dto.Name.Should().Be("Jane Doe");
        dto.ShippingAddress.Should().Be(new ShippingAddressDto("221B Baker Street", null, "London", "NW1 6XE", "United Kingdom"));
        dto.Memberships.Should().Equal(new MembershipDto(MembershipType.BookClub, TestClock.Now));
        dto.ActiveClubs.Should().Equal(Club.Book);
    }

    [Fact]
    public void Customer_without_address_maps_to_null_address()
    {
        CustomerMapper.ToDto(new CustomerBuilder().WithoutShippingAddress().Build()).ShippingAddress.Should().BeNull();
    }

    [Theory]
    [InlineData(Club.None, new Club[0])]
    [InlineData(Club.Book, new[] { Club.Book })]
    [InlineData(Club.Video, new[] { Club.Video })]
    [InlineData(Club.Book | Club.Video, new[] { Club.Book, Club.Video })]
    public void Clubs_are_expanded_into_individual_values(Club clubs, Club[] expected)
    {
        ClubsMapper.ToList(clubs).Should().Equal(expected);
    }

    [Fact]
    public void Shipping_slip_mapping_includes_items_and_address()
    {
        var customer = new CustomerBuilder().Build();
        var order = new PurchaseOrderBuilder().ForCustomer(customer).WithBook().WithBook().Build();
        var slip = ShippingSlipFactory.CreateFor(new ShippingSlipId(8), order, customer, TestClock.Now);

        var dto = ShippingSlipMapper.ToDto(slip);

        dto.Should().BeEquivalentTo(new ShippingSlipDto(
            8,
            order.Id.Value,
            customer.Id.Value,
            new ShippingAddressDto("221B Baker Street", null, "London", "NW1 6XE", "United Kingdom"),
            [new ShippingSlipItemDto(TestProducts.DefaultBookId, "The Girl on the train", 2)],
            TestClock.Now));
    }

    [Fact]
    public void Product_plan_activation_and_address_mappings_are_straightforward()
    {
        ProductMapper.ToDto(TestProducts.Video()).Should().Be(new ProductDto(1, "Comprehensive First Aid Training", ProductKind.Video, 19.50m, false));
        MembershipPlanMapper.ToDto(TestPlans.Premium()).Should().BeEquivalentTo(new MembershipPlanDto(MembershipType.Premium, "Premium Membership", 30.00m, [Club.Book, Club.Video]));
        MembershipActivationMapper.ToDto(new MembershipActivationResult(MembershipType.BookClub, MembershipActivationOutcome.AlreadyActive, null))
            .Should().Be(new MembershipActivationDto(MembershipType.BookClub, MembershipActivationOutcome.AlreadyActive, null));

        var address = TestAddresses.Kyiv();
        ShippingAddressMapper.ToDomain(ShippingAddressMapper.ToDto(address)).Should().Be(address);
    }

    [Fact]
    public void Mappers_reject_null_input()
    {
        var order = () => PurchaseOrderMapper.ToDto((PurchaseOrder)null!);
        var line = () => PurchaseOrderMapper.ToDto((OrderLine)null!);
        var customer = () => CustomerMapper.ToDto(null!);
        var product = () => ProductMapper.ToDto(null!);
        var plan = () => MembershipPlanMapper.ToDto(null!);
        var slip = () => ShippingSlipMapper.ToDto(null!);
        var activation = () => MembershipActivationMapper.ToDto(null!);
        var addressToDto = () => ShippingAddressMapper.ToDto(null!);
        var addressToDomain = () => ShippingAddressMapper.ToDomain(null!);

        order.Should().Throw<ArgumentNullException>();
        line.Should().Throw<ArgumentNullException>();
        customer.Should().Throw<ArgumentNullException>();
        product.Should().Throw<ArgumentNullException>();
        plan.Should().Throw<ArgumentNullException>();
        slip.Should().Throw<ArgumentNullException>();
        activation.Should().Throw<ArgumentNullException>();
        addressToDto.Should().Throw<ArgumentNullException>();
        addressToDomain.Should().Throw<ArgumentNullException>();
    }
}
