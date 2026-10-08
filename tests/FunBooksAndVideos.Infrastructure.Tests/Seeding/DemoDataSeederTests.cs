using FluentAssertions;
using FunBooksAndVideos.Domain.Catalog;
using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Infrastructure.Persistence;
using FunBooksAndVideos.Infrastructure.Seeding;
using FunBooksAndVideos.Infrastructure.Tests.Persistence;

namespace FunBooksAndVideos.Infrastructure.Tests.Seeding;

public sealed class DemoDataSeederTests
{
    private readonly InMemoryDataStore _store = new();
    private readonly DemoDataSeeder _seeder;

    public DemoDataSeederTests()
    {
        _seeder = new DemoDataSeeder(_store);
    }

    private StoreScope NewScope() => new(_store);

    [Fact]
    public async Task Seeds_the_kata_customer_with_a_shipping_address_and_a_digital_only_customer()
    {
        _seeder.Seed();
        var scope = NewScope();

        var jane = await scope.Customers.FindAsync(new CustomerId(DemoData.JaneDoeCustomerId), CancellationToken.None);
        var dan = await scope.Customers.FindAsync(new CustomerId(DemoData.DigitalOnlyCustomerId), CancellationToken.None);

        jane.Should().NotBeNull();
        jane!.Name.Should().Be(DemoData.JaneDoeName);
        jane.ShippingAddress.Should().NotBeNull();
        jane.Memberships.Should().BeEmpty();
        dan.Should().NotBeNull();
        dan!.ShippingAddress.Should().BeNull();
    }

    [Fact]
    public async Task Seeds_the_kata_products_and_the_membership_plans()
    {
        _seeder.Seed();
        var scope = NewScope();

        var products = await scope.Products.ListAsync(CancellationToken.None);
        var plans = await scope.MembershipPlans.ListAsync(CancellationToken.None);

        products.Select(product => (product.Id.Value, product.Kind, product.Name, product.Price.Amount)).Should().Equal(
            (DemoData.FirstAidVideoId, ProductKind.Video, DemoData.FirstAidVideoName, DemoData.FirstAidVideoPrice),
            (DemoData.GirlOnTheTrainBookId, ProductKind.Book, DemoData.GirlOnTheTrainBookName, DemoData.GirlOnTheTrainBookPrice),
            (DemoData.CleanCodeBookId, ProductKind.Book, DemoData.CleanCodeBookName, DemoData.CleanCodeBookPrice),
            (DemoData.YogaVideoId, ProductKind.Video, DemoData.YogaVideoName, DemoData.YogaVideoPrice));
        plans.Select(plan => (plan.Type, plan.Price.Amount)).Should().Equal(
            (MembershipType.BookClub, DemoData.BookClubPrice),
            (MembershipType.VideoClub, DemoData.VideoClubPrice),
            (MembershipType.Premium, DemoData.PremiumPrice));
    }

    [Fact]
    public async Task The_first_generated_identifiers_do_not_collide_with_seeded_ones()
    {
        _seeder.Seed();
        var scope = NewScope();

        (await scope.PurchaseOrders.NextIdentityAsync(CancellationToken.None)).Value.Should().Be(DemoData.FirstPurchaseOrderId);
        (await scope.Customers.NextIdentityAsync(CancellationToken.None)).Value.Should().Be(DemoData.DigitalOnlyCustomerId + 1);
        (await scope.Products.NextIdentityAsync(CancellationToken.None)).Value.Should().Be(DemoData.YogaVideoId + 1);
    }

    [Fact]
    public async Task Seeding_twice_changes_nothing()
    {
        _seeder.Seed();
        _seeder.Seed();
        var scope = NewScope();

        (await scope.Products.ListAsync(CancellationToken.None)).Should().HaveCount(4);
        (await scope.PurchaseOrders.NextIdentityAsync(CancellationToken.None)).Value.Should().Be(DemoData.FirstPurchaseOrderId);
    }

    [Fact]
    public void The_kata_example_total_is_forty_eight_fifty()
    {
        DemoData.KataExampleTotal.Should().Be(48.50m);
    }

    [Fact]
    public void Rejects_a_null_store()
    {
        var act = () => new DemoDataSeeder(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
