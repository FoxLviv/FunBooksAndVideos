using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using FunBooksAndVideos.Application.Customers.CreateCustomer;
using FunBooksAndVideos.Domain.Customers;

namespace FunBooksAndVideos.Api.Contracts;

/// <summary>Customer account to register.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CreateCustomerRequest
{
    /// <summary>Display name.</summary>
    /// <example>Jane Doe</example>
    [Required]
    [StringLength(Customer.MaxNameLength)]
    public string Name { get; init; } = string.Empty;

    /// <summary>Optional postal address. Without one the customer can only buy digital products and memberships.</summary>
    public ShippingAddressRequest? ShippingAddress { get; init; }

    public CreateCustomerCommand ToCommand() => new(Name, ShippingAddress?.ToDto());
}
