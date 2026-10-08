namespace FunBooksAndVideos.Domain.Customers;

/// <summary>Outcome of <see cref="Customer.ActivateMembership"/>.</summary>
/// <param name="Type">Membership that was requested.</param>
/// <param name="Outcome">Whether the account changed.</param>
/// <param name="ActivatedAt">Activation time when <paramref name="Outcome"/> is <see cref="MembershipActivationOutcome.Activated"/>; otherwise <see langword="null"/>.</param>
public sealed record MembershipActivationResult(
    MembershipType Type,
    MembershipActivationOutcome Outcome,
    DateTimeOffset? ActivatedAt);
