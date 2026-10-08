using FunBooksAndVideos.Application.Abstractions;
using FunBooksAndVideos.Domain.Persistence;
using Microsoft.Extensions.Logging;

namespace FunBooksAndVideos.Application.Decorators;

/// <summary>
/// GoF Decorator around a command handler: when the commit hits an optimistic concurrency conflict
/// the whole command is re-executed from scratch (fresh loads, fresh decisions) a bounded number of times.
/// The decorated handler stays free of retry logic.
/// </summary>
public sealed class ConcurrencyRetryDecorator<TCommand, TResult> : ICommandHandler<TCommand, TResult>
    where TCommand : notnull
{
    public const int DefaultMaxAttempts = 3;

    private readonly ICommandHandler<TCommand, TResult> _inner;
    private readonly ILogger<ConcurrencyRetryDecorator<TCommand, TResult>> _logger;
    private readonly int _maxAttempts;

    public ConcurrencyRetryDecorator(
        ICommandHandler<TCommand, TResult> inner,
        ILogger<ConcurrencyRetryDecorator<TCommand, TResult>> logger,
        int maxAttempts = DefaultMaxAttempts)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxAttempts, 1);

        _inner = inner;
        _logger = logger;
        _maxAttempts = maxAttempts;
    }

    public async Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await _inner.HandleAsync(command, cancellationToken);
            }
            catch (ConcurrencyConflictException ex) when (attempt < _maxAttempts)
            {
                ConcurrencyRetryLog.Retrying(_logger, attempt, _maxAttempts, ex.EntityName, ex.Key, ex);
            }
        }
    }
}

internal static partial class ConcurrencyRetryLog
{
    [LoggerMessage(EventId = 3000, Level = LogLevel.Warning, Message = "Attempt {Attempt}/{MaxAttempts} hit a concurrency conflict on {EntityName} {Key}; retrying.")]
    public static partial void Retrying(ILogger logger, int attempt, int maxAttempts, string entityName, object key, Exception exception);
}
