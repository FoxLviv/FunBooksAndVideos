using FluentAssertions;
using FunBooksAndVideos.Domain.Catalog;
using FunBooksAndVideos.Domain.Common;
using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Domain.ValueObjects;
using FunBooksAndVideos.TestKit;

namespace FunBooksAndVideos.Domain.Tests.Common;

public sealed class EntityTests
{
    [Fact]
    public void Entities_of_the_same_type_with_the_same_id_are_equal()
    {
        var first = TestProducts.Book(id: 7, name: "A");
        var second = TestProducts.Book(id: 7, name: "B");

        first.Should().Be(second);
        (first == second).Should().BeTrue();
        (first != second).Should().BeFalse();
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void Entities_of_different_types_with_the_same_id_are_not_equal()
    {
        Product book = TestProducts.Book(id: 7);
        Product video = TestProducts.Video(id: 7);

        book.Should().NotBe(video);
        (book == video).Should().BeFalse();
    }

    [Fact]
    public void Entities_with_different_ids_are_not_equal()
    {
        TestProducts.Book(id: 1).Should().NotBe(TestProducts.Book(id: 2));
    }

    [Fact]
    public void Null_comparisons_are_safe()
    {
        Book? book = TestProducts.Book();
        Book? nothing = null;

        (book == nothing).Should().BeFalse();
        (nothing == book).Should().BeFalse();
        (nothing == null).Should().BeTrue();
        book!.Equals(nothing).Should().BeFalse();
        book.Equals((object)"not an entity").Should().BeFalse();
    }

    [Fact]
    public void Aggregate_roots_reject_a_default_id()
    {
        var act = () => Customer.Create(default, "Jane", null);

        act.Should().Throw<DomainValidationException>().Which.Code.Should().Be("entity.id.invalid");
    }

    [Fact]
    public void Entities_reject_a_default_id()
    {
        var act = () => new Book(default, "The Girl on the train", Money.Of(14m));

        act.Should().Throw<DomainValidationException>().Which.Code.Should().Be("entity.id.invalid");
    }

    [Fact]
    public void Aggregate_roots_reject_negative_versions()
    {
        var act = () => Customer.Rehydrate(new CustomerId(1), "Jane", null, [], version: -1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void New_aggregates_start_at_the_initial_version()
    {
        new CustomerBuilder().Build().Version.Should().Be(AggregateRoot<CustomerId>.InitialVersion);
    }

    [Fact]
    public void Money_is_a_value_object_not_an_entity()
    {
        Money.Of(1m).Should().NotBeAssignableTo<Entity<ProductId>>();
    }
}
