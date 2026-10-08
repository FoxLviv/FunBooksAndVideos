using FunBooksAndVideos.Application.Exceptions;
using FunBooksAndVideos.Application.Idempotency;
using FunBooksAndVideos.Domain.Common;
using FunBooksAndVideos.Domain.Persistence;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace FunBooksAndVideos.Api.ErrorHandling;

/// <summary>
/// Translates domain, use-case and persistence exceptions into RFC 9457 problem details with a stable
/// <c>code</c> extension. Anything unknown becomes a generic 500 without leaking internals.
/// </summary>
internal sealed partial class ApiExceptionHandler : IExceptionHandler
{
    public const string CodeExtension = "code";

    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<ApiExceptionHandler> _logger;

    public ApiExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<ApiExceptionHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(problemDetailsService);
        ArgumentNullException.ThrowIfNull(logger);

        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        var mapping = Map(exception);

        if (mapping.Status < StatusCodes.Status500InternalServerError)
        {
            LogRequestRejected(_logger, mapping.Status, mapping.Code, exception.Message);
        }
        else
        {
            // Returning true below stops the exception handler middleware from logging, so log here.
            LogUnhandledException(_logger, exception, httpContext.Request.Method, httpContext.Request.Path);
        }

        var problem = mapping.Errors is null
            ? new ProblemDetails()
            : new ValidationProblemDetails(mapping.Errors);

        problem.Status = mapping.Status;
        problem.Title = mapping.Title;
        problem.Detail = mapping.Status < StatusCodes.Status500InternalServerError ? exception.Message : null;
        problem.Instance = httpContext.Request.Path;
        problem.Extensions[CodeExtension] = mapping.Code;

        await ProblemResponseWriter.WriteAsync(httpContext, problem, _problemDetailsService, exception);
        return true;
    }

    private static ErrorMapping Map(Exception exception) =>
        exception switch
        {
            ValidationException ex => new(StatusCodes.Status422UnprocessableEntity, "The request could not be processed.", ex.Code, ToDictionary(ex.Errors)),
            IdempotencyPayloadMismatchException ex => new(StatusCodes.Status409Conflict, "The idempotency key was already used with a different request.", ex.Code, null),
            NotFoundException ex => new(StatusCodes.Status404NotFound, "The requested resource was not found.", ex.Code, null),
            DomainValidationException ex => new(StatusCodes.Status422UnprocessableEntity, "The request violates a domain rule.", ex.Code, null),
            BusinessRuleViolationException ex => new(StatusCodes.Status422UnprocessableEntity, "The request violates a business rule.", ex.Code, null),
            DuplicateEntityException ex => new(StatusCodes.Status409Conflict, "The resource already exists.", ex.Code, null),
            ConcurrencyConflictException ex => new(StatusCodes.Status409Conflict, "The resource was modified concurrently. Please retry.", ex.Code, null),
            // Raised by the server while reading the request (e.g. the body exceeds Kestrel's size limit): a client error.
            BadHttpRequestException ex => new(ex.StatusCode, "The request could not be read.", ProblemCodes.ForStatus(ex.StatusCode), null),
            _ => new(StatusCodes.Status500InternalServerError, "An unexpected error occurred.", "internal_error", null),
        };

    private static Dictionary<string, string[]> ToDictionary(IReadOnlyDictionary<string, string[]> errors) =>
        errors.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);

    [LoggerMessage(EventId = 4000, Level = LogLevel.Information, Message = "Request rejected with {StatusCode} ({Code}): {Reason}")]
    private static partial void LogRequestRejected(ILogger logger, int statusCode, string code, string reason);

    [LoggerMessage(EventId = 4001, Level = LogLevel.Error, Message = "Unhandled exception while processing {Method} {Path}")]
    private static partial void LogUnhandledException(ILogger logger, Exception exception, string method, PathString path);

    private sealed record ErrorMapping(int Status, string Title, string Code, Dictionary<string, string[]>? Errors);
}
