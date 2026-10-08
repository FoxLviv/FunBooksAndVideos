using FluentAssertions;
using FunBooksAndVideos.Api.ErrorHandling;
using FunBooksAndVideos.Application.Exceptions;
using FunBooksAndVideos.Application.Idempotency;
using FunBooksAndVideos.Domain.Idempotency;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FunBooksAndVideos.Api.Tests.ErrorHandling;

public sealed class ApiExceptionHandlerTests
{
    private readonly RecordingProblemDetailsService _problemDetails = new();
    private readonly RecordingLogger _logger = new();

    [Fact]
    public async Task Unexpected_exceptions_become_a_generic_500_and_are_logged_as_errors()
    {
        var handler = new ApiExceptionHandler(_problemDetails, _logger);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = HttpMethods.Post;
        httpContext.Request.Path = "/api/v1/purchase-orders";
        var exception = new InvalidOperationException("boom");

        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        handled.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        var problem = _problemDetails.Received.Should().ContainSingle().Subject.ProblemDetails;
        problem.Status.Should().Be(StatusCodes.Status500InternalServerError);
        problem.Title.Should().Be("An unexpected error occurred.");
        problem.Extensions[ApiExceptionHandler.CodeExtension].Should().Be("internal_error");
        problem.Detail.Should().BeNull("internal details must not leak to the client");
        var entry = _logger.Entries.Should().ContainSingle(log => log.Level == LogLevel.Error).Subject;
        entry.Exception.Should().BeSameAs(exception);
    }

    [Fact]
    public async Task Expected_failures_are_logged_as_information_not_as_errors()
    {
        var handler = new ApiExceptionHandler(_problemDetails, _logger);
        var httpContext = new DefaultHttpContext();

        await handler.TryHandleAsync(httpContext, new NotFoundException("customer.not_found", "Customer 1 does not exist."), CancellationToken.None);

        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        _problemDetails.Received.Should().ContainSingle().Which.ProblemDetails.Detail.Should().Be("Customer 1 does not exist.");
        _logger.Entries.Should().ContainSingle().Which.Level.Should().Be(LogLevel.Information);
        _logger.Entries.Should().NotContain(log => log.Level == LogLevel.Error);
    }

    [Fact]
    public async Task Request_reading_failures_keep_their_client_status_instead_of_becoming_500()
    {
        var handler = new ApiExceptionHandler(_problemDetails, _logger);
        var httpContext = new DefaultHttpContext();

        await handler.TryHandleAsync(httpContext, new BadHttpRequestException("Request body too large.", StatusCodes.Status413PayloadTooLarge), CancellationToken.None);

        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status413PayloadTooLarge);
        var problem = _problemDetails.Received.Should().ContainSingle().Subject.ProblemDetails;
        problem.Title.Should().Be("The request could not be read.");
        problem.Detail.Should().Be("Request body too large.");
        problem.Extensions[ApiExceptionHandler.CodeExtension].Should().Be("request.too_large");
        _logger.Entries.Should().ContainSingle().Which.Level.Should().Be(LogLevel.Information);
    }

    [Fact]
    public async Task A_reused_idempotency_key_with_a_different_payload_is_a_409_with_its_own_code()
    {
        var handler = new ApiExceptionHandler(_problemDetails, _logger);
        var httpContext = new DefaultHttpContext();

        await handler.TryHandleAsync(httpContext, new IdempotencyPayloadMismatchException(new IdempotencyKey("order-42")), CancellationToken.None);

        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        var problem = _problemDetails.Received.Should().ContainSingle().Subject.ProblemDetails;
        problem.Title.Should().Be("The idempotency key was already used with a different request.");
        problem.Extensions[ApiExceptionHandler.CodeExtension].Should().Be("idempotency.payload_mismatch");
    }

    private sealed class RecordingProblemDetailsService : IProblemDetailsService
    {
        public List<ProblemDetailsContext> Received { get; } = [];

        public ValueTask WriteAsync(ProblemDetailsContext context)
        {
            Received.Add(context);
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> TryWriteAsync(ProblemDetailsContext context)
        {
            Received.Add(context);
            return ValueTask.FromResult(true);
        }
    }

    private sealed record LogEntry(LogLevel Level, EventId EventId, Exception? Exception, string Message);

    private sealed class RecordingLogger : ILogger<ApiExceptionHandler>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Add(new LogEntry(logLevel, eventId, exception, formatter(state, exception)));
    }
}
