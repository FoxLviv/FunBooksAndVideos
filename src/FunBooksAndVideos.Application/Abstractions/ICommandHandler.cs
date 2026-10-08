namespace FunBooksAndVideos.Application.Abstractions;

/// <summary>
/// GoF Command: a use case that changes state. One handler per command keeps every use case
/// independently testable and replaceable (and lets cross-cutting behaviour be added as decorators).
/// </summary>
public interface ICommandHandler<in TCommand, TResult>
    where TCommand : notnull
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
