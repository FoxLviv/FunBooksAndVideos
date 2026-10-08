using FluentAssertions;
using FunBooksAndVideos.Domain.Catalog;
using FunBooksAndVideos.Domain.Common;
using FunBooksAndVideos.Domain.ValueObjects;

namespace FunBooksAndVideos.Domain.Tests.Catalog;

public sealed class ProductTests
{
    private static readonly ProductId Id = new(1);

    [Fact]
    public void Books_are_physical_and_require_shipping()
    {
        var book = new Book(Id, "Clean Code", Money.Of(29.99m));

        book.Kind.Should().Be(ProductKind.Book);
        book.RequiresShipping.Should().BeTrue();
        book.Should().BeAssignableTo<PhysicalProduct>();
    }

    [Fact]
    public void Videos_are_digital_and_never_shipped()
    {
        var video = new Video(Id, "Yoga", Money.Of(9.99m));

        video.Kind.Should().Be(ProductKind.Video);
        video.RequiresShipping.Should().BeFalse();
        video.Should().BeAssignableTo<DigitalProduct>();
    }

    [Theory]
    [InlineData(ProductKind.Book, typeof(Book))]
    [InlineData(ProductKind.Video, typeof(Video))]
    public void Factory_creates_the_concrete_type_for_a_kind(ProductKind kind, Type expectedType)
    {
        var product = ProductFactory.Create(kind, Id, "Title", Money.Of(1m));

        product.Should().BeOfType(expectedType);
        product.Id.Should().Be(Id);
        product.Name.Should().Be("Title");
        product.Price.Should().Be(Money.Of(1m));
        product.Kind.Should().Be(kind);
    }

    [Fact]
    public void Factory_rejects_unknown_kinds()
    {
        var act = () => ProductFactory.Create((ProductKind)99, Id, "Title", Money.Of(1m));

        act.Should().Throw<DomainValidationException>().Which.Code.Should().Be("product.kind.unsupported");
    }

    [Fact]
    public void Name_is_trimmed()
    {
        new Book(Id, "  Clean Code  ", Money.Zero).Name.Should().Be("Clean Code");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Name_is_required(string? name)
    {
        var act = () => new Book(Id, name!, Money.Zero);

        act.Should().Throw<DomainValidationException>().Which.Code.Should().Be("product.name.invalid");
    }

    [Fact]
    public void Name_is_limited_in_length()
    {
        var act = () => new Video(Id, new string('a', Product.MaxNameLength + 1), Money.Zero);

        act.Should().Throw<DomainValidationException>().Which.Code.Should().Be("product.name.invalid");
    }

    [Fact]
    public void ToString_describes_kind_and_name()
    {
        new Book(Id, "The Girl on the train", Money.Zero).ToString().Should().Be("Book \"The Girl on the train\"");
    }

    [Fact]
    public void Product_ids_must_be_positive()
    {
        var zero = () => new ProductId(0);
        var negative = () => new ProductId(-5);

        zero.Should().Throw<DomainValidationException>().Which.Code.Should().Be("product_id.invalid");
        negative.Should().Throw<DomainValidationException>().Which.Code.Should().Be("product_id.invalid");
        new ProductId(5).ToString().Should().Be("5");
    }
}
