namespace FunBooksAndVideos.Domain.Customers;

/// <summary>Membership a customer can purchase.</summary>
public enum MembershipType
{
    /// <summary>Access to the book club.</summary>
    BookClub = 1,

    /// <summary>Access to the video club.</summary>
    VideoClub = 2,

    /// <summary>Access to both clubs.</summary>
    Premium = 3,
}
