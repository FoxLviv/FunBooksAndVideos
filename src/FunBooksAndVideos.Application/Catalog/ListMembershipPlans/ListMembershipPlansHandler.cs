using FunBooksAndVideos.Application.Abstractions;
using FunBooksAndVideos.Application.Dtos;
using FunBooksAndVideos.Application.Mapping;
using FunBooksAndVideos.Domain.Persistence;

namespace FunBooksAndVideos.Application.Catalog.ListMembershipPlans;

public sealed class ListMembershipPlansHandler : IQueryHandler<ListMembershipPlansQuery, IReadOnlyList<MembershipPlanDto>>
{
    private readonly IMembershipPlanRepository _membershipPlans;

    public ListMembershipPlansHandler(IMembershipPlanRepository membershipPlans)
    {
        ArgumentNullException.ThrowIfNull(membershipPlans);
        _membershipPlans = membershipPlans;
    }

    public async Task<IReadOnlyList<MembershipPlanDto>> HandleAsync(ListMembershipPlansQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var plans = await _membershipPlans.ListAsync(cancellationToken);
        return plans.Select(MembershipPlanMapper.ToDto).ToList();
    }
}
