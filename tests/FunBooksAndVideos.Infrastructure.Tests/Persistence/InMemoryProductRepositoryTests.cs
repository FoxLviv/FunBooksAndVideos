using FluentAssertions;
using FunBooksAndVideos.Domain.Catalog;
using FunBooksAndVideos.Domain.Persistence;
using FunBooksAndVideos.Infrastructure.Persistence;
using FunBooksAndVideos.TestKit;

namespace FunBooksAndVideos.Infrastructure.Tests.Persistence;

public sealed class InMemoryProductRepositoryTests
{
    private readonly InMemoryDataStore _store = new();

    public InMemoryProductRepositoryTests()
    {
        var scope = new StoreScope(_store);
        scope.Products.Add(TestProducts.Book(id: 2));
        scope.Products.Add(TestProducts.Video(id: 1));
        scope.Products.Add(TestProducts.Book(id: 3, name: "Clean Code", price: 29.99m));
        scope.CommitAsync().GetAwaiter().GetResult();
    }

    private StoreScope NewScope() => new(_store);

    [Fact]
    public async Task Find_returns_the_concrete_product_type()
    {
        var product = await NewScope().Products.FindAsync(new ProductId(2), CancellationToken.None);

        product.Should().BeOfType<Book>();
        product!.Name.Should().Be("The Girl on the train");
        product.RequiresShipping.Should().BeTrue();
        (await NewScope().Products.FindAsync(new ProductId(42), CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task FindMany_returns_only_the_products_that_exist_keyed_by_id()
    {
        var found = await NewScope().Products.FindManyAsync([new ProductId(1), new ProductId(42), new ProductId(2), new ProductId(1)], CancellationToken.None);

        found.Keys.Should().BeEquivalentTo([new ProductId(1), new ProductId(2)]);
        found[new ProductId(1)].Should().BeOfType<Video>();
        found[new ProductId(2)].Should().BeOfType<Book>();
    }

    [Fact]
    public async Task FindMany_with_no_ids_returns_an_empty_dictionary()
    {
        (await NewScope().Products.FindManyAsync([], CancellationToken.None)).Should().BeEmpty();
    }

    [Fact]
    public async Task List_returns_products_ordered_by_id()
    {
        var products = await NewScope().Products.ListAsync(CancellationToken.None);

        products.Select(product => product.Id.Value).Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task Add_requires_a_commit_and_rejects_duplicates()
    {
        var scope = NewScope();
        var newId = new ProductId(10);
        scope.Products.Add(new Video(newId, "New video", Domain.ValueObjects.Money.Of(5m)));

        (await NewScope().Products.FindAsync(newId, CancellationToken.None)).Should().BeNull();
        await scope.CommitAsync();
        (await NewScope().Products.FindAsync(newId, CancellationToken.None)).Should().NotBeNull();

        var duplicate = NewScope();
        duplicate.Products.Add(TestProducts.Book(id: 2));
        var act = () => duplicate.CommitAsync();
        await act.Should().ThrowAsync<DuplicateEntityException>().Where(ex => ex.EntityName == "Product");
    }

    [Fact]
    public async Task Identities_are_generated_in_increasing_order()
    {
        var scope = NewScope();

        var first = await scope.Products.NextIdentityAsync(CancellationToken.None);
        var second = await scope.Products.NextIdentityAsync(CancellationToken.None);

        second.Value.Should().BeGreaterThan(first.Value);
    }

    [Fact]
    public async Task Rejects_null_arguments()
    {
        var scope = NewScope();

        var nullIds = () => scope.Products.FindManyAsync(null!, CancellationToken.None);
        var nullProduct = () => scope.Products.Add(null!);

        await nullIds.Should().ThrowAsync<ArgumentNullException>();
        nullProduct.Should().Throw<ArgumentNullException>();
    }
}
