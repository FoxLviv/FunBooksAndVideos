using FunBooksAndVideos.Domain.Common;

namespace FunBooksAndVideos.Domain.ValueObjects;

/// <summary>Postal address a physical product is shipped to. Immutable value object.</summary>
public sealed record ShippingAddress
{
    public const int MaxLineLength = 200;
    public const int MaxCityLength = 100;
    public const int MaxPostalCodeLength = 20;
    public const int MaxCountryLength = 100;

    public ShippingAddress(string line1, string? line2, string city, string postalCode, string country)
    {
        Line1 = Guard.RequiredText(line1, MaxLineLength, "shipping_address.line1.invalid", "Address line 1");
        Line2 = Guard.OptionalText(line2, MaxLineLength, "shipping_address.line2.invalid", "Address line 2");
        City = Guard.RequiredText(city, MaxCityLength, "shipping_address.city.invalid", "City");
        PostalCode = Guard.RequiredText(postalCode, MaxPostalCodeLength, "shipping_address.postal_code.invalid", "Postal code");
        Country = Guard.RequiredText(country, MaxCountryLength, "shipping_address.country.invalid", "Country");
    }

    public string Line1 { get; }

    public string? Line2 { get; }

    public string City { get; }

    public string PostalCode { get; }

    public string Country { get; }
}
