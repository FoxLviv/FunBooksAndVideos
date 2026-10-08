using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Domain.Persistence;
using FunBooksAndVideos.Infrastructure.Persistence.Mapping;
using FunBooksAndVideos.Infrastructure.Persistence.Records;

namespace FunBooksAndVideos.Infrastructure.Persistence.Repositories;

internal sealed class InMemoryCustomerRepository : ICustomerRepository
{
    private const string EntityName = "Customer";

    private readonly InMemoryDataStore _store;
    private readonly InMemoryUnitOfWork _unitOfWork;

    public InMemoryCustomerRepository(InMemoryDataStore store, InMemoryUnitOfWork unitOfWork)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(unitOfWork);

        _store = store;
        _unitOfWork = unitOfWork;
    }

    public Task<Customer?> FindAsync(CustomerId id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var record = _store.Read(store => store.Customers.GetValueOrDefault(id.Value));
        return Task.FromResult(record is null ? null : CustomerRecordMapper.ToDomain(record));
    }

    public Task<CustomerId> NextIdentityAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new CustomerId(_store.NextCustomerId()));
    }

    public void Add(Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);

        var record = CustomerRecordMapper.ToRecord(customer, customer.Version);
        _unitOfWork.Enlist(new InsertOperation<long, CustomerRecord>(EntityName, store => store.Customers, record.Id, record));
    }

    public void Update(Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);

        var snapshot = CustomerRecordMapper.ToRecord(customer, customer.Version);
        _unitOfWork.Enlist(new UpdateOperation<long, CustomerRecord>(
            EntityName,
            store => store.Customers,
            snapshot.Id,
            customer.Version,
            newVersion => snapshot with { Version = newVersion }));
    }
}
