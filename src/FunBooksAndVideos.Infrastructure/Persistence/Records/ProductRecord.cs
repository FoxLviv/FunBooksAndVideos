using FunBooksAndVideos.Domain.Catalog;
using FunBooksAndVideos.Domain.ValueObjects;

namespace FunBooksAndVideos.Infrastructure.Persistence.Records;

/// <summary>Immutable stored snapshot of a catalog product.</summary>
internal sealed record ProductRecord(long Id, ProductKind Kind, string Name, Money Price);
