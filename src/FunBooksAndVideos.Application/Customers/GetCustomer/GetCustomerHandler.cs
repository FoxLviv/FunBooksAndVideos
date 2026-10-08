using FunBooksAndVideos.Application.Abstractions;
using FunBooksAndVideos.Application.Dtos;
using FunBooksAndVideos.Application.Exceptions;
using FunBooksAndVideos.Application.Mapping;
using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Domain.Persistence;

namespace FunBooksAndVideos.Application.Customers.GetCustomer;

public sealed class GetCustomerHandler : IQueryHandler<GetCustomerQuery, CustomerDto>
{
    private readonly ICustomerRepository _customers;

    public GetCustomerHandler(ICustomerRepository customers)
    {
        ArgumentNullException.ThrowIfNull(customers);
        _customers = customers;
    }

    public async Task<CustomerDto> HandleAsync(GetCustomerQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var customer = await _customers.FindAsync(new CustomerId(query.CustomerId), cancellationToken)
            ?? throw new NotFoundException("customer.not_found", $"Customer {query.CustomerId} was not found.");

        return CustomerMapper.ToDto(customer);
    }
}
