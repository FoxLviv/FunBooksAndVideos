using FunBooksAndVideos.Domain.Catalog;
using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Domain.Persistence;
using FunBooksAndVideos.Infrastructure.Persistence.Mapping;

namespace FunBooksAndVideos.Infrastructure.Persistence.Repositories;

internal sealed class InMemoryMembershipPlanRepository : IMembershipPlanRepository
{
    private readonly InMemoryDataStore _store;

    public InMemoryMembershipPlanRepository(InMemoryDataStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
    }

    public Task<MembershipPlan?> FindAsync(MembershipType type, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var record = _store.Read(store => store.MembershipPlans.GetValueOrDefault(type));
        return Task.FromResult(record is null ? null : MembershipPlanRecordMapper.ToDomain(record));
    }

    public Task<IReadOnlyList<MembershipPlan>> ListAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var records = _store.Read(store => store.MembershipPlans.Values.ToList());
        IReadOnlyList<MembershipPlan> plans = records.OrderBy(record => record.Type).Select(MembershipPlanRecordMapper.ToDomain).ToList();

        return Task.FromResult(plans);
    }
}
