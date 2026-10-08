using FluentAssertions;
using FunBooksAndVideos.Domain.Catalog;
using FunBooksAndVideos.Domain.Common;

namespace FunBooksAndVideos.Domain.Tests.Common;

public sealed class GuardTests
{
    [Fact]
    public void RequiredText_returns_the_trimmed_value()
    {
        Guard.RequiredText("  hello ", 10, "code", "Field").Should().Be("hello");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t ")]
    public void RequiredText_rejects_blank_values_with_the_given_code(string? value)
    {
        var act = () => Guard.RequiredText(value, 10, "field.invalid", "Field");

        act.Should().Throw<DomainValidationException>()
            .Which.Should().Match<DomainValidationException>(ex => ex.Code == "field.invalid" && ex.Message.Contains("Field is required"));
    }

    [Fact]
    public void RequiredText_rejects_values_longer_than_the_maximum_after_trimming()
    {
        Guard.RequiredText("  abc  ", 3, "code", "Field").Should().Be("abc");

        var act = () => Guard.RequiredText("abcd", 3, "code", "Field");

        act.Should().Throw<DomainValidationException>().Which.Message.Should().Contain("must not exceed 3");
    }

    [Fact]
    public void Text_of_exactly_the_maximum_length_is_accepted()
    {
        Guard.RequiredText("abc", 3, "code", "Field").Should().Be("abc");
        Guard.OptionalText("abc", 3, "code", "Field").Should().Be("abc");
    }

    [Fact]
    public void OptionalText_maps_blank_to_null_and_trims_otherwise()
    {
        Guard.OptionalText("   ", 10, "code", "Field").Should().BeNull();
        Guard.OptionalText(null, 10, "code", "Field").Should().BeNull();
        Guard.OptionalText(" x ", 10, "code", "Field").Should().Be("x");
    }

    [Fact]
    public void OptionalText_still_enforces_the_maximum_length()
    {
        var act = () => Guard.OptionalText("abcd", 3, "code", "Field");

        act.Should().Throw<DomainValidationException>().Which.Code.Should().Be("code");
    }

    [Fact]
    public void NotDefault_returns_a_non_default_value()
    {
        Guard.NotDefault(new ProductId(5), "code", "Product id").Should().Be(new ProductId(5));
    }

    [Fact]
    public void NotDefault_rejects_the_default_value_with_the_given_code()
    {
        var act = () => Guard.NotDefault(default(ProductId), "product_id.missing", "Product id");

        act.Should().Throw<DomainValidationException>()
            .Which.Should().Match<DomainValidationException>(ex => ex.Code == "product_id.missing" && ex.Message.Contains("Product id"));
    }

    [Fact]
    public void DefinedEnum_accepts_declared_members_and_rejects_others()
    {
        Guard.DefinedEnum(ProductKind.Book, "code").Should().Be(ProductKind.Book);

        var act = () => Guard.DefinedEnum((ProductKind)42, "product.kind.unsupported");

        act.Should().Throw<DomainValidationException>().Which.Code.Should().Be("product.kind.unsupported");
    }
}
