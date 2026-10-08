using FluentAssertions;
using FunBooksAndVideos.Application.Abstractions;
using FunBooksAndVideos.Application.Decorators;
using FunBooksAndVideos.Domain.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace FunBooksAndVideos.Application.Tests.Decorators;

public sealed class ConcurrencyRetryDecoratorTests
{
    private readonly ICommandHandler<string, int> _inner = Substitute.For<ICommandHandler<string, int>>();

    private ConcurrencyRetryDecorator<string, int> CreateDecorator(int maxAttempts = ConcurrencyRetryDecorator<string, int>.DefaultMaxAttempts) =>
        new(_inner, NullLogger<ConcurrencyRetryDecorator<string, int>>.Instance, maxAttempts);

    private static ConcurrencyConflictException Conflict() => new("Customer", 1L);

    [Fact]
    public async Task Returns_the_inner_result_when_the_first_attempt_succeeds()
    {
        _inner.HandleAsync("cmd", Arg.Any<CancellationToken>()).Returns(42);

        var result = await CreateDecorator().HandleAsync("cmd", CancellationToken.None);

        result.Should().Be(42);
        await _inner.Received(1).HandleAsync("cmd", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Re_executes_the_command_after_a_concurrency_conflict()
    {
        _inner.HandleAsync("cmd", Arg.Any<CancellationToken>()).Returns(
            _ => throw Conflict(),
            _ => throw Conflict(),
            _ => 42);

        var result = await CreateDecorator().HandleAsync("cmd", CancellationToken.None);

        result.Should().Be(42);
        await _inner.Received(3).HandleAsync("cmd", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Gives_up_after_the_maximum_number_of_attempts()
    {
        _inner.HandleAsync("cmd", Arg.Any<CancellationToken>()).Returns<int>(_ => throw Conflict());

        var act = () => CreateDecorator(maxAttempts: 3).HandleAsync("cmd", CancellationToken.None);

        await act.Should().ThrowAsync<ConcurrencyConflictException>();
        await _inner.Received(3).HandleAsync("cmd", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Does_not_retry_other_failures()
    {
        _inner.HandleAsync("cmd", Arg.Any<CancellationToken>()).Returns<int>(_ => throw new InvalidOperationException("boom"));

        var act = () => CreateDecorator().HandleAsync("cmd", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        await _inner.Received(1).HandleAsync("cmd", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_single_attempt_means_no_retry()
    {
        _inner.HandleAsync("cmd", Arg.Any<CancellationToken>()).Returns<int>(_ => throw Conflict());

        var act = () => CreateDecorator(maxAttempts: 1).HandleAsync("cmd", CancellationToken.None);

        await act.Should().ThrowAsync<ConcurrencyConflictException>();
        await _inner.Received(1).HandleAsync("cmd", Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Validates_its_arguments()
    {
        var nullInner = () => new ConcurrencyRetryDecorator<string, int>(null!, NullLogger<ConcurrencyRetryDecorator<string, int>>.Instance);
        var nullLogger = () => new ConcurrencyRetryDecorator<string, int>(_inner, null!);
        var zeroAttempts = () => CreateDecorator(maxAttempts: 0);

        nullInner.Should().Throw<ArgumentNullException>();
        nullLogger.Should().Throw<ArgumentNullException>();
        zeroAttempts.Should().Throw<ArgumentOutOfRangeException>();
    }
}
