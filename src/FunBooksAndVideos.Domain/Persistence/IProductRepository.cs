using FunBooksAndVideos.Domain.Catalog;

namespace FunBooksAndVideos.Domain.Persistence;

public interface IProductRepository
{
    Task<Product?> FindAsync(ProductId id, CancellationToken cancellationToken);

    /// <summary>Returns the products that exist among <paramref name="ids"/>, keyed by id. Unknown ids are simply absent.</summary>
    Task<IReadOnlyDictionary<ProductId, Product>> FindManyAsync(IEnumerable<ProductId> ids, CancellationToken cancellationToken);

    /// <summary>All products ordered by id.</summary>
    Task<IReadOnlyList<Product>> ListAsync(CancellationToken cancellationToken);

    Task<ProductId> NextIdentityAsync(CancellationToken cancellationToken);

    /// <summary>Stages the insert; applied by <see cref="IUnitOfWork.CommitAsync"/>.</summary>
    void Add(Product product);
}
