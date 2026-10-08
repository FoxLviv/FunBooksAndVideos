namespace FunBooksAndVideos.Infrastructure.Persistence.Records;

/// <summary>A stored snapshot that participates in optimistic concurrency.</summary>
internal interface IVersionedRecord
{
    int Version { get; }
}
