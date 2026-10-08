using FunBooksAndVideos.Domain.Catalog;
using FunBooksAndVideos.Domain.Common;
using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Domain.ValueObjects;
using FunBooksAndVideos.Infrastructure.Persistence;
using FunBooksAndVideos.Infrastructure.Persistence.Mapping;

namespace FunBooksAndVideos.Infrastructure.Seeding;

/// <summary>Builds the demo data through the domain model, so seeded data obeys every invariant.</summary>
internal sealed class DemoDataSeeder : IDemoDataSeeder
{
    private readonly InMemoryDataStore _store;

    public DemoDataSeeder(InMemoryDataStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
    }

    public void Seed()
    {
        var customers = new[]
        {
            Customer.Create(
                new CustomerId(DemoData.JaneDoeCustomerId),
                DemoData.JaneDoeName,
                new ShippingAddress("221B Baker Street", null, "London", "NW1 6XE", "United Kingdom")),
            Customer.Create(
                new CustomerId(DemoData.DigitalOnlyCustomerId),
                DemoData.DigitalOnlyCustomerName,
                shippingAddress: null),
        };

        var products = new[]
        {
            ProductFactory.Create(ProductKind.Video, new ProductId(DemoData.FirstAidVideoId), DemoData.FirstAidVideoName, Money.Of(DemoData.FirstAidVideoPrice)),
            ProductFactory.Create(ProductKind.Book, new ProductId(DemoData.GirlOnTheTrainBookId), DemoData.GirlOnTheTrainBookName, Money.Of(DemoData.GirlOnTheTrainBookPrice)),
            ProductFactory.Create(ProductKind.Book, new ProductId(DemoData.CleanCodeBookId), DemoData.CleanCodeBookName, Money.Of(DemoData.CleanCodeBookPrice)),
            ProductFactory.Create(ProductKind.Video, new ProductId(DemoData.YogaVideoId), DemoData.YogaVideoName, Money.Of(DemoData.YogaVideoPrice)),
        };

        var plans = new[]
        {
            new MembershipPlan(MembershipType.BookClub, MembershipType.BookClub.GetDisplayName(), Money.Of(DemoData.BookClubPrice)),
            new MembershipPlan(MembershipType.VideoClub, MembershipType.VideoClub.GetDisplayName(), Money.Of(DemoData.VideoClubPrice)),
            new MembershipPlan(MembershipType.Premium, MembershipType.Premium.GetDisplayName(), Money.Of(DemoData.PremiumPrice)),
        };

        _store.Seed(
            customers.Select(customer => CustomerRecordMapper.ToRecord(customer, AggregateRoot<CustomerId>.InitialVersion)),
            products.Select(ProductRecordMapper.ToRecord),
            plans.Select(MembershipPlanRecordMapper.ToRecord),
            DemoData.FirstPurchaseOrderId);
    }
}
