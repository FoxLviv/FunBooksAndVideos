using FunBooksAndVideos.Domain.Catalog;
using FunBooksAndVideos.Infrastructure.Persistence.Records;

namespace FunBooksAndVideos.Infrastructure.Persistence.Mapping;

internal static class ProductRecordMapper
{
    public static ProductRecord ToRecord(Product product) =>
        new(product.Id.Value, product.Kind, product.Name, product.Price);

    public static Product ToDomain(ProductRecord record) =>
        ProductFactory.Create(record.Kind, new ProductId(record.Id), record.Name, record.Price);
}
