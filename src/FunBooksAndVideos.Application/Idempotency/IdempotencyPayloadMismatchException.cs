using FunBooksAndVideos.Application.Exceptions;
using FunBooksAndVideos.Domain.Idempotency;

namespace FunBooksAndVideos.Application.Idempotency;

/// <summary>An idempotency key was reused for a request whose payload differs from the original one.</summary>
public sealed class IdempotencyPayloadMismatchException : UseCaseException
{
    public const string ErrorCode = "idempotency.payload_mismatch";

    public IdempotencyPayloadMismatchException(IdempotencyKey key)
        : base(ErrorCode, $"Idempotency key '{key}' was already used with a different request payload.")
    {
        Key = key;
    }

    /// <summary>The reused key.</summary>
    public IdempotencyKey Key { get; }
}
