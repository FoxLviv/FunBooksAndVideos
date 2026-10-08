using FunBooksAndVideos.Application.Dtos;
using FunBooksAndVideos.Domain.Customers;

namespace FunBooksAndVideos.Application.Mapping;

public static class MembershipActivationMapper
{
    public static MembershipActivationDto ToDto(MembershipActivationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new MembershipActivationDto(result.Type, result.Outcome, result.ActivatedAt);
    }
}
