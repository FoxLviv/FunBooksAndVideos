namespace FunBooksAndVideos.Infrastructure.Persistence.Records;

/// <summary>Immutable stored snapshot of an idempotency record. Write-once, therefore not versioned.</summary>
internal sealed record IdempotencyRecordRecord(
    string Key,
    string RequestFingerprint,
    string ResponsePayload,
    DateTimeOffset CreatedAt);
