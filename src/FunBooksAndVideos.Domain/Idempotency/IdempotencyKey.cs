using FunBooksAndVideos.Domain.Common;

namespace FunBooksAndVideos.Domain.Idempotency;

/// <summary>
/// Client-supplied token that identifies one logical request across retries (the <c>Idempotency-Key</c> header).
/// Surrounding whitespace is trimmed; the remaining value must be 1 to <see cref="MaxLength"/> visible ASCII characters.
/// </summary>
public readonly record struct IdempotencyKey
{
    /// <summary>Maximum number of characters of a key.</summary>
    public const int MaxLength = 128;

    private const string ErrorCode = "idempotency_key.invalid";

    public IdempotencyKey(string value)
    {
        var trimmed = Guard.RequiredText(value, MaxLength, ErrorCode, "Idempotency key");

        foreach (var character in trimmed)
        {
            if (character is < '\x21' or > '\x7E')
            {
                throw new DomainValidationException(ErrorCode, "Idempotency key must consist of visible ASCII characters only.");
            }
        }

        Value = trimmed;
    }

    public string Value { get; }

    public override string ToString() => Value;
}
