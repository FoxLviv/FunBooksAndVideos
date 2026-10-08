namespace FunBooksAndVideos.Domain.Customers;

/// <summary>What happened when a membership was activated on an account.</summary>
public enum MembershipActivationOutcome
{
    /// <summary>The membership granted access to at least one club the customer did not have.</summary>
    Activated = 1,

    /// <summary>The customer already had access to every club the membership grants; nothing changed.</summary>
    AlreadyActive = 2,
}
