using FunBooksAndVideos.Application.Dtos;
using FunBooksAndVideos.Domain.Customers;

namespace FunBooksAndVideos.Application.Mapping;

public static class CustomerMapper
{
    public static CustomerDto ToDto(Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);

        return new CustomerDto(
            customer.Id.Value,
            customer.Name,
            customer.ShippingAddress is null ? null : ShippingAddressMapper.ToDto(customer.ShippingAddress),
            customer.Memberships.Select(membership => new MembershipDto(membership.Type, membership.ActivatedAt)).ToList(),
            ClubsMapper.ToList(customer.ActiveClubs));
    }
}
