using FunBooksAndVideos.Application.Dtos;
using FunBooksAndVideos.Domain.ValueObjects;

namespace FunBooksAndVideos.Application.Mapping;

public static class ShippingAddressMapper
{
    public static ShippingAddressDto ToDto(ShippingAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);
        return new ShippingAddressDto(address.Line1, address.Line2, address.City, address.PostalCode, address.Country);
    }

    public static ShippingAddress ToDomain(ShippingAddressDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        return new ShippingAddress(dto.Line1, dto.Line2, dto.City, dto.PostalCode, dto.Country);
    }
}
