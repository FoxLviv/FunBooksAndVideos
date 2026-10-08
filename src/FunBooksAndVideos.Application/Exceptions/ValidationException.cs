namespace FunBooksAndVideos.Application.Exceptions;

/// <summary>
/// The request is well-formed but semantically invalid (references an unknown product, total mismatch, ...).
/// <see cref="Errors"/> is keyed by the offending request member, e.g. <c>lines[1].productId</c>.
/// </summary>
public sealed class ValidationException : UseCaseException
{
    public const string ErrorCode = "validation.failed";

    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base(ErrorCode, "One or more validation errors occurred.")
    {
        ArgumentNullException.ThrowIfNull(errors);
        Errors = errors;
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public static ValidationException For(string member, string message) =>
        new ValidationErrors().Add(member, message).ToException();
}
