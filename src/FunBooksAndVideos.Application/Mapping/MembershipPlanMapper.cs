using FunBooksAndVideos.Application.Dtos;
using FunBooksAndVideos.Domain.Catalog;
using FunBooksAndVideos.Domain.Customers;

namespace FunBooksAndVideos.Application.Mapping;

public static class MembershipPlanMapper
{
    public static MembershipPlanDto ToDto(MembershipPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        return new MembershipPlanDto(plan.Type, plan.Name, plan.Price.Amount, ClubsMapper.ToList(plan.Type.GetClubs()));
    }
}
