using FunBooksAndVideos.Domain.Common;

namespace FunBooksAndVideos.Domain.Idempotency;

/// <summary>
/// Remembers the outcome of a request submitted with an <see cref="IdempotencyKey"/>: a fingerprint of the
/// request payload (to detect a key reused for a different request) and the serialized response to replay.
/// </summary>
public sealed class IdempotencyRecord
{
    /// <summary>Entity name used in persistence error messages (e.g. <see cref="Persistence.DuplicateEntityException"/>).</summary>
    public const string EntityName = "Idempotency record";

    /// <summary>Maximum length of a request fingerprint.</summary>
    public const int MaxFingerprintLength = 256;

    public IdempotencyRecord(IdempotencyKey key, string requestFingerprint, string responsePayload, DateTimeOffset createdAt)
    {
        Key = Guard.NotDefault(key, "idempotency_record.key.invalid", "Idempotency key");
        RequestFingerprint = Guard.RequiredText(requestFingerprint, MaxFingerprintLength, "idempotency_record.fingerprint.invalid", "Request fingerprint");
        ResponsePayload = string.IsNullOrWhiteSpace(responsePayload)
            ? throw new DomainValidationException("idempotency_record.response.invalid", "Response payload is required.")
            : responsePayload;
        CreatedAt = createdAt;
    }

    public IdempotencyKey Key { get; }

    public string RequestFingerprint { get; }

    public string ResponsePayload { get; }

    public DateTimeOffset CreatedAt { get; }
}
