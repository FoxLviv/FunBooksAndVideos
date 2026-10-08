using FunBooksAndVideos.Application.Abstractions;
using FunBooksAndVideos.Application.Dtos;
using FunBooksAndVideos.Application.Mapping;
using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Domain.Persistence;

namespace FunBooksAndVideos.Application.Customers.CreateCustomer;

public sealed class CreateCustomerHandler : ICommandHandler<CreateCustomerCommand, CustomerDto>
{
    private readonly ICustomerRepository _customers;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCustomerHandler(ICustomerRepository customers, IUnitOfWork unitOfWork)
    {
        ArgumentNullException.ThrowIfNull(customers);
        ArgumentNullException.ThrowIfNull(unitOfWork);

        _customers = customers;
        _unitOfWork = unitOfWork;
    }

    public async Task<CustomerDto> HandleAsync(CreateCustomerCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var address = command.ShippingAddress is null ? null : ShippingAddressMapper.ToDomain(command.ShippingAddress);
        var id = await _customers.NextIdentityAsync(cancellationToken);
        var customer = Customer.Create(id, command.Name, address);

        _customers.Add(customer);
        await _unitOfWork.CommitAsync(cancellationToken);

        return CustomerMapper.ToDto(customer);
    }
}
