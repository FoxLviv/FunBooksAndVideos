using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using FunBooksAndVideos.Application.Dtos;
using FunBooksAndVideos.Domain.ValueObjects;

namespace FunBooksAndVideos.Api.Contracts;

/// <summary>Postal address.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ShippingAddressRequest
{
    /// <summary>Street and number.</summary>
    /// <example>221B Baker Street</example>
    [Required]
    [StringLength(ShippingAddress.MaxLineLength)]
    public string Line1 { get; init; } = string.Empty;

    /// <summary>Apartment, floor, company; optional.</summary>
    [StringLength(ShippingAddress.MaxLineLength)]
    public string? Line2 { get; init; }

    /// <summary>City.</summary>
    /// <example>London</example>
    [Required]
    [StringLength(ShippingAddress.MaxCityLength)]
    public string City { get; init; } = string.Empty;

    /// <summary>Postal code.</summary>
    /// <example>NW1 6XE</example>
    [Required]
    [StringLength(ShippingAddress.MaxPostalCodeLength)]
    public string PostalCode { get; init; } = string.Empty;

    /// <summary>Country.</summary>
    /// <example>United Kingdom</example>
    [Required]
    [StringLength(ShippingAddress.MaxCountryLength)]
    public string Country { get; init; } = string.Empty;

    public ShippingAddressDto ToDto() => new(Line1, Line2, City, PostalCode, Country);
}
