using FluentAssertions;
using FunBooksAndVideos.Domain.Common;
using FunBooksAndVideos.Domain.ValueObjects;

namespace FunBooksAndVideos.Domain.Tests.ValueObjects;

public sealed class ShippingAddressTests
{
    [Fact]
    public void Trims_every_component()
    {
        var address = new ShippingAddress("  221B Baker Street ", "  Flat 1 ", " London ", " NW1 6XE ", " United Kingdom ");

        address.Line1.Should().Be("221B Baker Street");
        address.Line2.Should().Be("Flat 1");
        address.City.Should().Be("London");
        address.PostalCode.Should().Be("NW1 6XE");
        address.Country.Should().Be("United Kingdom");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_second_line_becomes_null(string? line2)
    {
        var address = new ShippingAddress("221B Baker Street", line2, "London", "NW1 6XE", "United Kingdom");

        address.Line2.Should().BeNull();
    }

    [Theory]
    [InlineData("", "London", "NW1 6XE", "UK", "shipping_address.line1.invalid")]
    [InlineData("Street", " ", "NW1 6XE", "UK", "shipping_address.city.invalid")]
    [InlineData("Street", "London", "", "UK", "shipping_address.postal_code.invalid")]
    [InlineData("Street", "London", "NW1 6XE", null, "shipping_address.country.invalid")]
    public void Rejects_blank_required_components(string? line1, string? city, string? postalCode, string? country, string expectedCode)
    {
        var act = () => new ShippingAddress(line1!, null, city!, postalCode!, country!);

        act.Should().Throw<DomainValidationException>().Which.Code.Should().Be(expectedCode);
    }

    [Fact]
    public void Rejects_components_that_are_too_long()
    {
        var tooLong = new string('x', ShippingAddress.MaxPostalCodeLength + 1);

        var act = () => new ShippingAddress("Street", null, "London", tooLong, "UK");

        act.Should().Throw<DomainValidationException>().Which.Code.Should().Be("shipping_address.postal_code.invalid");
    }

    [Fact]
    public void Is_a_value_object_compared_by_content()
    {
        var first = new ShippingAddress("Street", null, "London", "NW1 6XE", "UK");
        var second = new ShippingAddress("Street ", null, " London", "NW1 6XE", "UK");

        first.Should().Be(second);
    }
}
