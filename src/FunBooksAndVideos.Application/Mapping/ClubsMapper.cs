using FunBooksAndVideos.Domain.Customers;

namespace FunBooksAndVideos.Application.Mapping;

public static class ClubsMapper
{
    /// <summary>Expands a <see cref="Club"/> flag set into the individual clubs, excluding <see cref="Club.None"/>.</summary>
    public static IReadOnlyList<Club> ToList(Club clubs) =>
        Enum.GetValues<Club>()
            .Where(club => club != Club.None && clubs.HasFlag(club))
            .ToList();
}
