using FluentAssertions;
using FunBooksAndVideos.Domain.Common;
using FunBooksAndVideos.Domain.Persistence;

namespace FunBooksAndVideos.Domain.Tests.Common;

public sealed class DomainExceptionTests
{
    [Fact]
    public void Domain_exceptions_expose_code_and_message()
    {
        var exception = new BusinessRuleViolationException("some.code", "Some message.");

        exception.Code.Should().Be("some.code");
        exception.Message.Should().Be("Some message.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Domain_exceptions_require_a_code(string? code)
    {
        var act = () => new DomainValidationException(code!, "message");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Persistence_exceptions_describe_the_entity_and_key()
    {
        var conflict = new ConcurrencyConflictException("Customer", 42L);
        var duplicate = new DuplicateEntityException("Purchase order", 7L);

        conflict.Code.Should().Be(ConcurrencyConflictException.ErrorCode);
        conflict.EntityName.Should().Be("Customer");
        conflict.Key.Should().Be(42L);
        conflict.Message.Should().Contain("Customer 42");

        duplicate.Code.Should().Be(DuplicateEntityException.ErrorCode);
        duplicate.EntityName.Should().Be("Purchase order");
        duplicate.Key.Should().Be(7L);
        duplicate.Message.Should().Contain("Purchase order 7");
    }
}
