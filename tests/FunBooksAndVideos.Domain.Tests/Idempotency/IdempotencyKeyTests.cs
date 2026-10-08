using FluentAssertions;
using FunBooksAndVideos.Domain.Common;
using FunBooksAndVideos.Domain.Idempotency;

namespace FunBooksAndVideos.Domain.Tests.Idempotency;

public sealed class IdempotencyKeyTests
{
    [Fact]
    public void Trims_surrounding_whitespace()
    {
        var key = new IdempotencyKey("  order-42 ");

        key.Value.Should().Be("order-42");
        key.ToString().Should().Be("order-42");
    }

    [Fact]
    public void Accepts_every_visible_ascii_character_and_the_maximum_length()
    {
        var allVisible = new string(Enumerable.Range(0x21, 0x7E - 0x21 + 1).Select(code => (char)code).ToArray());
        var longest = new string('k', IdempotencyKey.MaxLength);

        new IdempotencyKey(allVisible).Value.Should().Be(allVisible);
        new IdempotencyKey(longest).Value.Should().HaveLength(IdempotencyKey.MaxLength);
        new IdempotencyKey("x").Value.Should().Be("x");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_blank_keys(string? value)
    {
        var act = () => new IdempotencyKey(value!);

        act.Should().Throw<DomainValidationException>().Which.Code.Should().Be("idempotency_key.invalid");
    }

    [Fact]
    public void Rejects_keys_longer_than_the_maximum()
    {
        var act = () => new IdempotencyKey(new string('k', IdempotencyKey.MaxLength + 1));

        act.Should().Throw<DomainValidationException>().Which.Code.Should().Be("idempotency_key.invalid");
    }

    [Theory]
    [InlineData("two words")]
    [InlineData("tab\there")]
    [InlineData("line\nbreak")]
    [InlineData("bell\u0007")]
    [InlineData("del\u007F")]
    [InlineData("café")]
    [InlineData("emoji\U0001F600")]
    public void Rejects_whitespace_control_and_non_ascii_characters(string value)
    {
        var act = () => new IdempotencyKey(value);

        act.Should().Throw<DomainValidationException>().Which.Code.Should().Be("idempotency_key.invalid");
    }

    [Fact]
    public void Is_a_value_object_compared_by_content()
    {
        new IdempotencyKey("abc").Should().Be(new IdempotencyKey(" abc "));
        new IdempotencyKey("abc").Should().NotBe(new IdempotencyKey("ABC"));
    }
}
