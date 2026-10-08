using FunBooksAndVideos.Domain.Idempotency;
using FunBooksAndVideos.Infrastructure.Persistence.Records;

namespace FunBooksAndVideos.Infrastructure.Persistence.Mapping;

internal static class IdempotencyRecordRecordMapper
{
    public static IdempotencyRecordRecord ToRecord(IdempotencyRecord record) =>
        new(record.Key.Value, record.RequestFingerprint, record.ResponsePayload, record.CreatedAt);

    public static IdempotencyRecord ToDomain(IdempotencyRecordRecord record) =>
        new(new IdempotencyKey(record.Key), record.RequestFingerprint, record.ResponsePayload, record.CreatedAt);
}
