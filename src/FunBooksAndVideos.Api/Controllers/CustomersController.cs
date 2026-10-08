using System.Net.Mime;
using FunBooksAndVideos.Api.Contracts;
using FunBooksAndVideos.Application.Abstractions;
using FunBooksAndVideos.Application.Customers.CreateCustomer;
using FunBooksAndVideos.Application.Customers.GetCustomer;
using FunBooksAndVideos.Application.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace FunBooksAndVideos.Api.Controllers;

/// <summary>Customer accounts and their memberships.</summary>
[ApiController]
[Route("api/v1/customers")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, MediaTypeNames.Application.ProblemJson)]
public sealed class CustomersController : ControllerBase
{
    private readonly ICommandHandler<CreateCustomerCommand, CustomerDto> _createCustomer;
    private readonly IQueryHandler<GetCustomerQuery, CustomerDto> _getCustomer;

    public CustomersController(
        ICommandHandler<CreateCustomerCommand, CustomerDto> createCustomer,
        IQueryHandler<GetCustomerQuery, CustomerDto> getCustomer)
    {
        ArgumentNullException.ThrowIfNull(createCustomer);
        ArgumentNullException.ThrowIfNull(getCustomer);

        _createCustomer = createCustomer;
        _getCustomer = getCustomer;
    }

    /// <summary>Registers a customer account.</summary>
    /// <response code="201">Customer created. The <c>Location</c> header points at the customer.</response>
    /// <response code="400">Malformed request.</response>
    /// <response code="422">The data violates a domain rule (e.g. blank name).</response>
    [HttpPost]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<CustomerDto>(StatusCodes.Status201Created, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, MediaTypeNames.Application.ProblemJson)]
    public async Task<ActionResult<CustomerDto>> Create([FromBody] CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var customer = await _createCustomer.HandleAsync(request.ToCommand(), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer);
    }

    /// <summary>Returns a customer with the memberships activated on the account (BR1).</summary>
    /// <param name="id">Customer id.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <response code="200">The customer.</response>
    /// <response code="404">No such customer.</response>
    [HttpGet("{id:long:min(1)}")]
    [ProducesResponseType<CustomerDto>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    public async Task<ActionResult<CustomerDto>> GetById(long id, CancellationToken cancellationToken) =>
        await _getCustomer.HandleAsync(new GetCustomerQuery(id), cancellationToken);
}
