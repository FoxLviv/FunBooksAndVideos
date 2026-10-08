namespace FunBooksAndVideos.Infrastructure.Seeding;

/// <summary>
/// Identifiers and prices of the demo data. The customer, products and the first purchase order id
/// reproduce the example from the kata: order 3344656 for customer 4567890 with a total of 48.50.
/// </summary>
public static class DemoData
{
    public const long JaneDoeCustomerId = 4567890;
    public const string JaneDoeName = "Jane Doe";

    /// <summary>A customer without a shipping address; buying physical products for this account fails on purpose.</summary>
    public const long DigitalOnlyCustomerId = 4567891;
    public const string DigitalOnlyCustomerName = "Dan Digital";

    public const long FirstAidVideoId = 1;
    public const string FirstAidVideoName = "Comprehensive First Aid Training";
    public const decimal FirstAidVideoPrice = 19.50m;

    public const long GirlOnTheTrainBookId = 2;
    public const string GirlOnTheTrainBookName = "The Girl on the train";
    public const decimal GirlOnTheTrainBookPrice = 14.00m;

    public const long CleanCodeBookId = 3;
    public const string CleanCodeBookName = "Clean Code";
    public const decimal CleanCodeBookPrice = 29.99m;

    public const long YogaVideoId = 4;
    public const string YogaVideoName = "Yoga for Beginners";
    public const decimal YogaVideoPrice = 9.99m;

    public const decimal BookClubPrice = 15.00m;
    public const decimal VideoClubPrice = 20.00m;
    public const decimal PremiumPrice = 30.00m;

    /// <summary>Identifier the first submitted purchase order receives.</summary>
    public const long FirstPurchaseOrderId = 3344656;

    /// <summary>Video + book + book club membership, as in the kata.</summary>
    public const decimal KataExampleTotal = FirstAidVideoPrice + GirlOnTheTrainBookPrice + BookClubPrice;
}
