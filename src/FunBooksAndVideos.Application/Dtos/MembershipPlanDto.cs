using FunBooksAndVideos.Domain.Customers;

namespace FunBooksAndVideos.Application.Dtos;

/// <summary>A membership that can be bought.</summary>
/// <param name="Type">Membership type to reference in a purchase order line.</param>
/// <param name="Name">Display name.</param>
/// <param name="Price">Price.</param>
/// <param name="Clubs">Clubs the membership grants access to.</param>
public sealed record MembershipPlanDto(MembershipType Type, string Name, decimal Price, IReadOnlyList<Club> Clubs);
