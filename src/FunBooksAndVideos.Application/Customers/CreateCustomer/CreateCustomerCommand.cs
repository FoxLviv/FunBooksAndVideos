using FunBooksAndVideos.Application.Dtos;

namespace FunBooksAndVideos.Application.Customers.CreateCustomer;

/// <summary>Registers a customer account.</summary>
/// <param name="Name">Display name.</param>
/// <param name="ShippingAddress">Optional postal address; required before physical products can be bought.</param>
public sealed record CreateCustomerCommand(string Name, ShippingAddressDto? ShippingAddress);
