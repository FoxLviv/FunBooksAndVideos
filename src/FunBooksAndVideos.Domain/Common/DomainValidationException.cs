namespace FunBooksAndVideos.Domain.Common;

/// <summary>
/// Raised when a value handed to the domain does not satisfy the structural rules of the domain
/// (negative money, blank names, overlapping memberships in one order, ...).
/// </summary>
public sealed class DomainValidationException : DomainException
{
    public DomainValidationException(string code, string message)
        : base(code, message)
    {
    }
}
