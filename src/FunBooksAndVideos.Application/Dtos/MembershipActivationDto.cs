using FunBooksAndVideos.Domain.Customers;

namespace FunBooksAndVideos.Application.Dtos;

/// <summary>What happened to one membership line of a processed purchase order.</summary>
/// <param name="MembershipType">Membership that was requested.</param>
/// <param name="Outcome">Activated, or already active (account unchanged).</param>
/// <param name="ActivatedAt">Activation time when activated; otherwise <see langword="null"/>.</param>
public sealed record MembershipActivationDto(MembershipType MembershipType, MembershipActivationOutcome Outcome, DateTimeOffset? ActivatedAt);
