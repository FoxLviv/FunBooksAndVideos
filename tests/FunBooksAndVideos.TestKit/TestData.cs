using FunBooksAndVideos.Domain.Catalog;
using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Domain.ValueObjects;

namespace FunBooksAndVideos.TestKit;

public static class TestAddresses
{
    public static ShippingAddress London() =>
        new("221B Baker Street", null, "London", "NW1 6XE", "United Kingdom");

    public static ShippingAddress Kyiv() =>
        new("Khreshchatyk St, 1", "Office 7", "Kyiv", "01001", "Ukraine");
}

public static class TestProducts
{
    public const long DefaultBookId = 2;
    public const long DefaultVideoId = 1;

    public static Book Book(long id = DefaultBookId, string name = "The Girl on the train", decimal price = 14.00m) =>
        new(new ProductId(id), name, Money.Of(price));

    public static Video Video(long id = DefaultVideoId, string name = "Comprehensive First Aid Training", decimal price = 19.50m) =>
        new(new ProductId(id), name, Money.Of(price));
}

public static class TestPlans
{
    public const decimal BookClubPrice = 15.00m;
    public const decimal VideoClubPrice = 20.00m;
    public const decimal PremiumPrice = 30.00m;

    public static MembershipPlan BookClub() => For(MembershipType.BookClub);

    public static MembershipPlan VideoClub() => For(MembershipType.VideoClub);

    public static MembershipPlan Premium() => For(MembershipType.Premium);

    public static MembershipPlan For(MembershipType type) =>
        type switch
        {
            MembershipType.BookClub => new MembershipPlan(type, type.GetDisplayName(), Money.Of(BookClubPrice)),
            MembershipType.VideoClub => new MembershipPlan(type, type.GetDisplayName(), Money.Of(VideoClubPrice)),
            MembershipType.Premium => new MembershipPlan(type, type.GetDisplayName(), Money.Of(PremiumPrice)),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown membership type."),
        };

    public static IReadOnlyList<MembershipPlan> All() => [BookClub(), VideoClub(), Premium()];
}
