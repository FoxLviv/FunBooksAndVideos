using FunBooksAndVideos.Domain.Customers;

namespace FunBooksAndVideos.Domain.Persistence;

public interface ICustomerRepository
{
    /// <summary>Returns a fresh instance of the customer, or <see langword="null"/> when unknown.</summary>
    Task<Customer?> FindAsync(CustomerId id, CancellationToken cancellationToken);

    Task<CustomerId> NextIdentityAsync(CancellationToken cancellationToken);

    /// <summary>Stages the insert; applied by <see cref="IUnitOfWork.CommitAsync"/>.</summary>
    void Add(Customer customer);

    /// <summary>Stages the update; applied by <see cref="IUnitOfWork.CommitAsync"/> if the stored version still matches.</summary>
    void Update(Customer customer);
}
