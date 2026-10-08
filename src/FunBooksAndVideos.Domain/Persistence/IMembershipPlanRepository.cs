using FunBooksAndVideos.Domain.Catalog;
using FunBooksAndVideos.Domain.Customers;

namespace FunBooksAndVideos.Domain.Persistence;

public interface IMembershipPlanRepository
{
    Task<MembershipPlan?> FindAsync(MembershipType type, CancellationToken cancellationToken);

    /// <summary>All plans ordered by membership type.</summary>
    Task<IReadOnlyList<MembershipPlan>> ListAsync(CancellationToken cancellationToken);
}
