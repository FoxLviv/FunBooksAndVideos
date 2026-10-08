namespace FunBooksAndVideos.Application.Abstractions;

/// <summary>A read-only use case.</summary>
public interface IQueryHandler<in TQuery, TResult>
    where TQuery : notnull
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
