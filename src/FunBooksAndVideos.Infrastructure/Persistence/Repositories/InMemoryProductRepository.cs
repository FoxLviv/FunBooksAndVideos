using FunBooksAndVideos.Domain.Catalog;
using FunBooksAndVideos.Domain.Persistence;
using FunBooksAndVideos.Infrastructure.Persistence.Mapping;
using FunBooksAndVideos.Infrastructure.Persistence.Records;

namespace FunBooksAndVideos.Infrastructure.Persistence.Repositories;

internal sealed class InMemoryProductRepository : IProductRepository
{
    private const string EntityName = "Product";

    private readonly InMemoryDataStore _store;
    private readonly InMemoryUnitOfWork _unitOfWork;

    public InMemoryProductRepository(InMemoryDataStore store, InMemoryUnitOfWork unitOfWork)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(unitOfWork);

        _store = store;
        _unitOfWork = unitOfWork;
    }

    public Task<Product?> FindAsync(ProductId id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var record = _store.Read(store => store.Products.GetValueOrDefault(id.Value));
        return Task.FromResult(record is null ? null : ProductRecordMapper.ToDomain(record));
    }

    public Task<IReadOnlyDictionary<ProductId, Product>> FindManyAsync(IEnumerable<ProductId> ids, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ids);
        cancellationToken.ThrowIfCancellationRequested();

        var distinctIds = ids.Distinct().ToList();
        var records = _store.Read(store => distinctIds
            .Select(id => store.Products.GetValueOrDefault(id.Value))
            .Where(record => record is not null)
            .Select(record => record!)
            .ToList());

        IReadOnlyDictionary<ProductId, Product> products = records
            .Select(ProductRecordMapper.ToDomain)
            .ToDictionary(product => product.Id);

        return Task.FromResult(products);
    }

    public Task<IReadOnlyList<Product>> ListAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var records = _store.Read(store => store.Products.Values.ToList());
        IReadOnlyList<Product> products = records.OrderBy(record => record.Id).Select(ProductRecordMapper.ToDomain).ToList();

        return Task.FromResult(products);
    }

    public Task<ProductId> NextIdentityAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new ProductId(_store.NextProductId()));
    }

    public void Add(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        var record = ProductRecordMapper.ToRecord(product);
        _unitOfWork.Enlist(new InsertOperation<long, ProductRecord>(EntityName, store => store.Products, record.Id, record));
    }
}
