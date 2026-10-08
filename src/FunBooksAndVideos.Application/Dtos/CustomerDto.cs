using FunBooksAndVideos.Domain.Customers;

namespace FunBooksAndVideos.Application.Dtos;

/// <summary>Customer account.</summary>
/// <param name="Id">Customer id.</param>
/// <param name="Name">Display name.</param>
/// <param name="ShippingAddress">Where physical products are sent; <see langword="null"/> when the customer has none.</param>
/// <param name="Memberships">Memberships in activation order.</param>
/// <param name="ActiveClubs">Clubs the customer currently has access to.</param>
public sealed record CustomerDto(
    long Id,
    string Name,
    ShippingAddressDto? ShippingAddress,
    IReadOnlyList<MembershipDto> Memberships,
    IReadOnlyList<Club> ActiveClubs);

/// <summary>An activated membership.</summary>
/// <param name="Type">Membership type.</param>
/// <param name="ActivatedAt">When it was activated (UTC).</param>
public sealed record MembershipDto(MembershipType Type, DateTimeOffset ActivatedAt);
