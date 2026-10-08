using FunBooksAndVideos.Domain.Catalog;
using FunBooksAndVideos.Infrastructure.Persistence.Records;

namespace FunBooksAndVideos.Infrastructure.Persistence.Mapping;

internal static class MembershipPlanRecordMapper
{
    public static MembershipPlanRecord ToRecord(MembershipPlan plan) =>
        new(plan.Type, plan.Name, plan.Price);

    public static MembershipPlan ToDomain(MembershipPlanRecord record) =>
        new(record.Type, record.Name, record.Price);
}
