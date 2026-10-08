using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Infrastructure.Persistence.Records;

namespace FunBooksAndVideos.Infrastructure.Persistence.Mapping;

internal static class CustomerRecordMapper
{
    public static CustomerRecord ToRecord(Customer customer, int version) =>
        new(customer.Id.Value, customer.Name, customer.ShippingAddress, customer.Memberships.ToArray(), version);

    public static Customer ToDomain(CustomerRecord record) =>
        Customer.Rehydrate(new CustomerId(record.Id), record.Name, record.ShippingAddress, record.Memberships, record.Version);
}
