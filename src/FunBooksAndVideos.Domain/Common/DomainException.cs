namespace FunBooksAndVideos.Domain.Common;

/// <summary>
/// Base type for coded domain failures (argument guards still throw the usual framework exceptions).
/// Carries a stable, machine-readable <see cref="Code"/> that API clients can rely on.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string code, string message)
        : base(message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        Code = code;
    }

    /// <summary>Stable error code, e.g. <c>purchase_order.lines.empty</c>.</summary>
    public string Code { get; }
}
