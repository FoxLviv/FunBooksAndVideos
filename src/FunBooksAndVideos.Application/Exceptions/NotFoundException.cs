namespace FunBooksAndVideos.Application.Exceptions;

/// <summary>The resource addressed by the request does not exist.</summary>
public sealed class NotFoundException : UseCaseException
{
    public NotFoundException(string code, string message)
        : base(code, message)
    {
    }
}
