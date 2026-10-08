namespace FunBooksAndVideos.Application.Exceptions;

/// <summary>Base type for failures reported by use cases. Carries a stable, machine-readable <see cref="Code"/>.</summary>
public abstract class UseCaseException : Exception
{
    protected UseCaseException(string code, string message)
        : base(message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        Code = code;
    }

    public string Code { get; }
}
