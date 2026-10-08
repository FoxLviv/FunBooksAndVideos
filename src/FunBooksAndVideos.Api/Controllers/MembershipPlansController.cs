using System.Net.Mime;
using FunBooksAndVideos.Application.Abstractions;
using FunBooksAndVideos.Application.Catalog.ListMembershipPlans;
using FunBooksAndVideos.Application.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace FunBooksAndVideos.Api.Controllers;

/// <summary>Memberships that can be bought: book club, video club or premium (both).</summary>
[ApiController]
[Route("api/v1/membership-plans")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, MediaTypeNames.Application.ProblemJson)]
public sealed class MembershipPlansController : ControllerBase
{
    private readonly IQueryHandler<ListMembershipPlansQuery, IReadOnlyList<MembershipPlanDto>> _listMembershipPlans;

    public MembershipPlansController(IQueryHandler<ListMembershipPlansQuery, IReadOnlyList<MembershipPlanDto>> listMembershipPlans)
    {
        ArgumentNullException.ThrowIfNull(listMembershipPlans);
        _listMembershipPlans = listMembershipPlans;
    }

    /// <summary>Lists the membership plans with their prices and the clubs they grant access to.</summary>
    /// <response code="200">Plans ordered by type.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<MembershipPlanDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    public async Task<ActionResult<IReadOnlyList<MembershipPlanDto>>> List(CancellationToken cancellationToken) =>
        Ok(await _listMembershipPlans.HandleAsync(new ListMembershipPlansQuery(), cancellationToken));
}
