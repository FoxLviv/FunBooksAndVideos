namespace FunBooksAndVideos.Domain.Common;

/// <summary>
/// Raised when an operation is structurally valid but violates a business invariant
/// in the current state (processing an already processed order, shipping without an address, ...).
/// </summary>
public sealed class BusinessRuleViolationException : DomainException
{
    public BusinessRuleViolationException(string code, string message)
        : base(code, message)
    {
    }
}
