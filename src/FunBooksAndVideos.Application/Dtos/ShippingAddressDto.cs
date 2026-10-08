namespace FunBooksAndVideos.Application.Dtos;

/// <summary>Postal address.</summary>
/// <param name="Line1">Street and number.</param>
/// <param name="Line2">Apartment, floor, company; optional.</param>
/// <param name="City">City.</param>
/// <param name="PostalCode">Postal code.</param>
/// <param name="Country">Country.</param>
public sealed record ShippingAddressDto(string Line1, string? Line2, string City, string PostalCode, string Country);
