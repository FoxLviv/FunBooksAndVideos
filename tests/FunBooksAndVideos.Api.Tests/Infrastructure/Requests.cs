namespace FunBooksAndVideos.Api.Tests.Infrastructure;

/// <summary>Request bodies as anonymous objects, so that malformed shapes can be sent as easily as valid ones.</summary>
internal static class Requests
{
    public static object Product(long productId) => new { type = "Product", productId };

    public static object Membership(string membershipType) => new { type = "Membership", membershipType };

    public static object Order(long customerId, decimal? expectedTotal, params object[] lines) =>
        new { customerId, lines, expectedTotal };

    public static object Customer(string name = "Ada Lovelace", bool withAddress = true) =>
        withAddress
            ? new { name, shippingAddress = Address() }
            : new { name };

    public static object Address(string line1 = "12 St James's Square", string city = "London", string postalCode = "SW1Y 4JH", string country = "United Kingdom") =>
        new { line1, city, postalCode, country };

    public static object ProductToCreate(string kind, string name, decimal price) => new { kind, name, price };
}
