using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Domain.ValueObjects;

namespace FunBooksAndVideos.TestKit;

/// <summary>Test data builder (GoF Builder) for customers. Defaults to the kata customer with a shipping address.</summary>
public sealed class CustomerBuilder
{
    public const long DefaultId = 4567890;

    private readonly List<Membership> _memberships = [];
    private long _id = DefaultId;
    private string _name = "Jane Doe";
    private ShippingAddress? _shippingAddress = TestAddresses.London();
    private int _version;

    public CustomerBuilder WithId(long id)
    {
        _id = id;
        return this;
    }

    public CustomerBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public CustomerBuilder WithShippingAddress(ShippingAddress? address)
    {
        _shippingAddress = address;
        return this;
    }

    public CustomerBuilder WithoutShippingAddress() => WithShippingAddress(null);

    public CustomerBuilder WithMembership(MembershipType type, DateTimeOffset? activatedAt = null)
    {
        _memberships.Add(new Membership(type, activatedAt ?? TestClock.Now.AddDays(-30)));
        return this;
    }

    public CustomerBuilder WithVersion(int version)
    {
        _version = version;
        return this;
    }

    public Customer Build() =>
        _memberships.Count == 0 && _version == 0
            ? Customer.Create(new CustomerId(_id), _name, _shippingAddress)
            : Customer.Rehydrate(new CustomerId(_id), _name, _shippingAddress, _memberships, _version);
}
