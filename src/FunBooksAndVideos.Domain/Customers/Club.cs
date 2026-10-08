namespace FunBooksAndVideos.Domain.Customers;

/// <summary>Clubs a customer can have access to. Flags, because premium members belong to both.</summary>
[Flags]
public enum Club
{
    None = 0,
    Book = 1,
    Video = 2,
}
