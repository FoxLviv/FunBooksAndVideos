namespace FunBooksAndVideos.Domain.Catalog;

/// <summary>Concrete kind of product sold by the shop.</summary>
public enum ProductKind
{
    /// <summary>Printed book, shipped to the customer.</summary>
    Book = 1,

    /// <summary>Online video, watched in the browser.</summary>
    Video = 2,
}
