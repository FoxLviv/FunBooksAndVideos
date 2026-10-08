using System.Collections.ObjectModel;
using FunBooksAndVideos.Domain.Common;
using FunBooksAndVideos.Domain.ValueObjects;

namespace FunBooksAndVideos.Domain.Customers;

/// <summary>
/// Customer account. Aggregate root that owns the customer's memberships and guarantees that
/// activating a membership is idempotent with respect to club access.
/// </summary>
public sealed class Customer : AggregateRoot<CustomerId>
{
    public const int MaxNameLength = 200;

    private readonly List<Membership> _memberships;
    private readonly ReadOnlyCollection<Membership> _readOnlyMemberships;

    private Customer(
        CustomerId id,
        string name,
        ShippingAddress? shippingAddress,
        IEnumerable<Membership> memberships,
        int version)
        : base(id, version)
    {
        Name = Guard.RequiredText(name, MaxNameLength, "customer.name.invalid", "Customer name");
        ShippingAddress = shippingAddress;
        _memberships = memberships.ToList();
        _readOnlyMemberships = _memberships.AsReadOnly();

        if (_memberships.Any(membership => membership is null))
        {
            throw new DomainValidationException("customer.memberships.invalid", "Memberships must not contain null entries.");
        }

        ActiveClubs = _memberships.Aggregate(Club.None, (clubs, membership) => clubs | membership.Clubs);
    }

    public string Name { get; }

    /// <summary>Where physical products are sent. Optional: customers of digital products only never need one.</summary>
    public ShippingAddress? ShippingAddress { get; }

    /// <summary>Memberships in activation order.</summary>
    public IReadOnlyList<Membership> Memberships => _readOnlyMemberships;

    /// <summary>Union of the clubs granted by all memberships.</summary>
    public Club ActiveClubs { get; private set; }

    public static Customer Create(CustomerId id, string name, ShippingAddress? shippingAddress) =>
        new(id, name, shippingAddress, [], InitialVersion);

    /// <summary>Rebuilds a customer from a persisted snapshot. Only the persistence layer should call this.</summary>
    public static Customer Rehydrate(
        CustomerId id,
        string name,
        ShippingAddress? shippingAddress,
        IEnumerable<Membership> memberships,
        int version)
    {
        ArgumentNullException.ThrowIfNull(memberships);
        return new Customer(id, name, shippingAddress, memberships, version);
    }

    public bool HasAccessTo(Club club) => club != Club.None && (ActiveClubs & club) == club;

    /// <summary>
    /// Activates a membership. When the customer already has access to every club the membership grants,
    /// the account is left untouched and <see cref="MembershipActivationOutcome.AlreadyActive"/> is reported.
    /// A membership that adds at least one new club (for example premium on top of book club) is activated.
    /// </summary>
    public MembershipActivationResult ActivateMembership(MembershipType type, DateTimeOffset activatedAt)
    {
        var requestedClubs = type.GetClubs();
        if (HasAccessTo(requestedClubs))
        {
            return new MembershipActivationResult(type, MembershipActivationOutcome.AlreadyActive, null);
        }

        _memberships.Add(new Membership(type, activatedAt));
        ActiveClubs |= requestedClubs;
        return new MembershipActivationResult(type, MembershipActivationOutcome.Activated, activatedAt);
    }
}
