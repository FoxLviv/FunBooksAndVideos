using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using FunBooksAndVideos.Api.Json;
using FunBooksAndVideos.Application.Dtos;
using FunBooksAndVideos.Application.PurchaseOrders.SubmitPurchaseOrder;
using FunBooksAndVideos.Domain.Customers;

namespace FunBooksAndVideos.Api.Contracts;

/// <summary>
/// One item line. Set <c>type</c> to <c>Product</c> together with <c>productId</c>,
/// or to <c>Membership</c> together with <c>membershipType</c>. Unknown members and the property of the
/// other branch are rejected while the JSON is read (see <see cref="OrderLineRequestModelJsonConverter"/>).
/// </summary>
[JsonConverter(typeof(OrderLineRequestModelJsonConverter))]
public sealed class OrderLineRequestModel : IValidatableObject
{
    /// <summary>Product or Membership.</summary>
    /// <example>Product</example>
    [Required]
    [EnumDataType(typeof(OrderLineType))]
    public OrderLineType? Type { get; init; }

    /// <summary>Catalog product id. Required for product lines, forbidden otherwise.</summary>
    /// <example>2</example>
    [Range(typeof(long), "1", "9223372036854775807", ParseLimitsInInvariantCulture = true, ErrorMessage = "The product id must be a positive number.")]
    public long? ProductId { get; init; }

    /// <summary>Membership to buy. Required for membership lines, forbidden otherwise.</summary>
    /// <example>BookClub</example>
    [EnumDataType(typeof(MembershipType))]
    public MembershipType? MembershipType { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Type == OrderLineType.Product && ProductId is null)
        {
            yield return new ValidationResult("A product line requires a productId.", [nameof(ProductId)]);
        }

        if (Type == OrderLineType.Membership && MembershipType is null)
        {
            yield return new ValidationResult("A membership line requires a membershipType.", [nameof(MembershipType)]);
        }
    }

    public OrderLineRequest ToRequest() =>
        Type switch
        {
            OrderLineType.Product when ProductId is { } productId => new ProductLineRequest(productId),
            OrderLineType.Membership when MembershipType is { } membershipType => new MembershipLineRequest(membershipType),
            _ => throw new InvalidOperationException("The order line was not validated before conversion."),
        };
}
