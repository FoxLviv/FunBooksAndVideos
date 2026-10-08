using FluentAssertions;
using FunBooksAndVideos.Application.Customers.CreateCustomer;
using FunBooksAndVideos.Application.Customers.GetCustomer;
using FunBooksAndVideos.Application.Dtos;
using FunBooksAndVideos.Application.Exceptions;
using FunBooksAndVideos.Domain.Common;
using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Domain.Persistence;
using FunBooksAndVideos.TestKit;
using NSubstitute;

namespace FunBooksAndVideos.Application.Tests.Customers;

public sealed class CustomerHandlerTests
{
    private static readonly ShippingAddressDto Address = new("221B Baker Street", null, "London", "NW1 6XE", "United Kingdom");

    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    public CustomerHandlerTests()
    {
        _customers.NextIdentityAsync(Arg.Any<CancellationToken>()).Returns(new CustomerId(5));
    }

    [Fact]
    public async Task CreateCustomer_stores_the_customer_with_its_address_and_commits()
    {
        var handler = new CreateCustomerHandler(_customers, _unitOfWork);

        var dto = await handler.HandleAsync(new CreateCustomerCommand(" Ada Lovelace ", Address), CancellationToken.None);

        dto.Should().BeEquivalentTo(new CustomerDto(5, "Ada Lovelace", Address, [], []));
        _customers.Received(1).Add(Arg.Is<Customer>(customer => customer.Id.Value == 5 && customer.ShippingAddress == TestAddresses.London()));
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateCustomer_without_address_is_allowed()
    {
        var handler = new CreateCustomerHandler(_customers, _unitOfWork);

        var dto = await handler.HandleAsync(new CreateCustomerCommand("Dan Digital", null), CancellationToken.None);

        dto.ShippingAddress.Should().BeNull();
        _customers.Received(1).Add(Arg.Is<Customer>(customer => customer.ShippingAddress == null));
    }

    [Fact]
    public async Task CreateCustomer_rejects_invalid_data_before_touching_persistence()
    {
        var handler = new CreateCustomerHandler(_customers, _unitOfWork);

        var act = () => handler.HandleAsync(new CreateCustomerCommand("  ", null), CancellationToken.None);

        await act.Should().ThrowAsync<DomainValidationException>().Where(ex => ex.Code == "customer.name.invalid");
        _customers.DidNotReceive().Add(Arg.Any<Customer>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCustomer_maps_memberships_and_active_clubs()
    {
        var customer = new CustomerBuilder().WithMembership(MembershipType.Premium, TestClock.Now).Build();
        _customers.FindAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);

        var dto = await new GetCustomerHandler(_customers).HandleAsync(new GetCustomerQuery(customer.Id.Value), CancellationToken.None);

        dto.Id.Should().Be(customer.Id.Value);
        dto.Name.Should().Be("Jane Doe");
        dto.ShippingAddress.Should().Be(Address);
        dto.Memberships.Should().ContainSingle().Which.Should().Be(new MembershipDto(MembershipType.Premium, TestClock.Now));
        dto.ActiveClubs.Should().Equal(Club.Book, Club.Video);
    }

    [Fact]
    public async Task GetCustomer_reports_unknown_customers()
    {
        var act = () => new GetCustomerHandler(_customers).HandleAsync(new GetCustomerQuery(404), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().Where(ex => ex.Code == "customer.not_found");
    }

    [Fact]
    public async Task Handlers_reject_null_arguments()
    {
        var nullRepository = () => new CreateCustomerHandler(null!, _unitOfWork);
        var nullUnitOfWork = () => new CreateCustomerHandler(_customers, null!);
        var nullQueryRepository = () => new GetCustomerHandler(null!);
        var nullCommand = () => new CreateCustomerHandler(_customers, _unitOfWork).HandleAsync(null!, CancellationToken.None);
        var nullQuery = () => new GetCustomerHandler(_customers).HandleAsync(null!, CancellationToken.None);

        nullRepository.Should().Throw<ArgumentNullException>();
        nullUnitOfWork.Should().Throw<ArgumentNullException>();
        nullQueryRepository.Should().Throw<ArgumentNullException>();
        await nullCommand.Should().ThrowAsync<ArgumentNullException>();
        await nullQuery.Should().ThrowAsync<ArgumentNullException>();
    }
}
