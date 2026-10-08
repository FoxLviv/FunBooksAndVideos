using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Domain.ValueObjects;

namespace FunBooksAndVideos.Infrastructure.Persistence.Records;

/// <summary>Immutable stored snapshot of a membership plan.</summary>
internal sealed record MembershipPlanRecord(MembershipType Type, string Name, Money Price);
