using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Domain.ValueObjects;

namespace FunBooksAndVideos.Infrastructure.Persistence.Records;

/// <summary>Immutable stored snapshot of a customer. Memberships are immutable value objects and can be shared safely.</summary>
internal sealed record CustomerRecord(
    long Id,
    string Name,
    ShippingAddress? ShippingAddress,
    IReadOnlyList<Membership> Memberships,
    int Version) : IVersionedRecord;
