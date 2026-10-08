using FluentAssertions;
using FunBooksAndVideos.Application.Exceptions;

namespace FunBooksAndVideos.Application.Tests.Exceptions;

public sealed class ValidationErrorsTests
{
    [Fact]
    public void Groups_messages_by_member_and_keeps_their_order()
    {
        var errors = new ValidationErrors()
            .Add("lines[0].productId", "first")
            .Add("customerId", "second")
            .Add("lines[0].productId", "third");

        errors.HasErrors.Should().BeTrue();

        var exception = errors.ToException();

        exception.Code.Should().Be(ValidationException.ErrorCode);
        exception.Errors.Should().HaveCount(2);
        exception.Errors["lines[0].productId"].Should().Equal("first", "third");
        exception.Errors["customerId"].Should().Equal("second");
    }

    [Fact]
    public void ThrowIfAny_only_throws_when_there_are_errors()
    {
        var empty = new ValidationErrors();
        var filled = new ValidationErrors().Add("x", "y");

        var noThrow = () => empty.ThrowIfAny();
        var throws = () => filled.ThrowIfAny();

        empty.HasErrors.Should().BeFalse();
        noThrow.Should().NotThrow();
        throws.Should().Throw<ValidationException>().Which.Errors.Should().ContainKey("x");
    }

    [Fact]
    public void For_creates_a_single_error()
    {
        var exception = ValidationException.For("expectedTotal", "mismatch");

        exception.Errors.Should().ContainSingle().Which.Should().BeEquivalentTo(new KeyValuePair<string, string[]>("expectedTotal", ["mismatch"]));
        exception.Message.Should().Contain("validation errors");
    }

    [Fact]
    public void Rejects_blank_members_messages_and_null_dictionaries()
    {
        var blankMember = () => new ValidationErrors().Add(" ", "message");
        var blankMessage = () => new ValidationErrors().Add("member", "");
        var nullErrors = () => new ValidationException(null!);
        var blankCode = () => new NotFoundException("", "message");

        blankMember.Should().Throw<ArgumentException>();
        blankMessage.Should().Throw<ArgumentException>();
        nullErrors.Should().Throw<ArgumentNullException>();
        blankCode.Should().Throw<ArgumentException>();
    }
}
