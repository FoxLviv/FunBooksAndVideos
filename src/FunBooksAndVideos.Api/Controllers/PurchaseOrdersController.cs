using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using FunBooksAndVideos.Api.Contracts;
using FunBooksAndVideos.Application.Abstractions;
using FunBooksAndVideos.Application.Dtos;
using FunBooksAndVideos.Application.PurchaseOrders.GetPurchaseOrder;
using FunBooksAndVideos.Application.PurchaseOrders.GetShippingSlip;
using FunBooksAndVideos.Application.PurchaseOrders.SubmitPurchaseOrder;
using FunBooksAndVideos.Domain.Idempotency;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FunBooksAndVideos.Api.Controllers;

/// <summary>Purchase orders and the artefacts their processing produces.</summary>
[ApiController]
[Route("api/v1/purchase-orders")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, MediaTypeNames.Application.ProblemJson)]
public sealed class PurchaseOrdersController : ControllerBase
{
    /// <summary>Request header carrying the client's idempotency key.</summary>
    public const string IdempotencyKeyHeader = "Idempotency-Key";

    /// <summary>Response header set to <c>true</c> when the result was replayed for a repeated idempotency key.</summary>
    public const string IdempotentReplayedHeader = "Idempotent-Replayed";

    private readonly ICommandHandler<SubmitPurchaseOrderCommand, SubmitPurchaseOrderResult> _submitPurchaseOrder;
    private readonly IQueryHandler<GetPurchaseOrderQuery, PurchaseOrderDto> _getPurchaseOrder;
    private readonly IQueryHandler<GetShippingSlipQuery, ShippingSlipDto> _getShippingSlip;
    private readonly ApiBehaviorOptions _apiBehavior;

    public PurchaseOrdersController(
        ICommandHandler<SubmitPurchaseOrderCommand, SubmitPurchaseOrderResult> submitPurchaseOrder,
        IQueryHandler<GetPurchaseOrderQuery, PurchaseOrderDto> getPurchaseOrder,
        IQueryHandler<GetShippingSlipQuery, ShippingSlipDto> getShippingSlip,
        IOptions<ApiBehaviorOptions> apiBehavior)
    {
        ArgumentNullException.ThrowIfNull(submitPurchaseOrder);
        ArgumentNullException.ThrowIfNull(getPurchaseOrder);
        ArgumentNullException.ThrowIfNull(getShippingSlip);
        ArgumentNullException.ThrowIfNull(apiBehavior);

        _submitPurchaseOrder = submitPurchaseOrder;
        _getPurchaseOrder = getPurchaseOrder;
        _getShippingSlip = getShippingSlip;
        _apiBehavior = apiBehavior.Value;
    }

    /// <summary>Submits a purchase order and processes it immediately.</summary>
    /// <remarks>
    /// Runs the purchase order processor:
    /// BR1 - every membership line is activated on the customer account (idempotent: an already active membership is reported as such);
    /// BR2 - a shipping slip is generated when the order contains physical products (books).
    /// The order id and the total are assigned by the server. Everything is persisted atomically: if any rule fails, nothing is stored.
    /// </remarks>
    /// <param name="request">The order to submit.</param>
    /// <param name="idempotencyKey">
    /// Optional <c>Idempotency-Key</c> header (1 to 128 visible ASCII characters) that makes retries safe:
    /// repeating the request with the same key and the same body creates no second order but replays the original
    /// result (<c>201</c>, same <c>Location</c>, response header <c>Idempotent-Replayed: true</c>);
    /// the same key with a different body is rejected with <c>409</c>.
    /// </param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <response code="201">Order processed (or replayed for a repeated idempotency key). The <c>Location</c> header points at the order.</response>
    /// <response code="400">Malformed request (missing fields, unknown enum values, empty lines, invalid <c>Idempotency-Key</c> header).</response>
    /// <response code="422">Unknown customer or product, overlapping memberships in one order, total mismatch, or physical products for a customer without a shipping address.</response>
    /// <response code="409">The customer account was modified concurrently and the retries were exhausted (retry the request), or the <c>Idempotency-Key</c> was already used with a different request body (<c>idempotency.payload_mismatch</c>).</response>
    [HttpPost]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<SubmitPurchaseOrderResult>(StatusCodes.Status201Created, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status422UnprocessableEntity, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, MediaTypeNames.Application.ProblemJson)]
    public async Task<IActionResult> Submit(
        [FromBody] SubmitPurchaseOrderRequest request,
        [FromHeader(Name = IdempotencyKeyHeader)]
        [StringLength(IdempotencyKey.MaxLength, MinimumLength = 1)]
        [RegularExpression("^[\\x21-\\x7E]+$", ErrorMessage = "The Idempotency-Key must consist of 1 to 128 visible ASCII characters.")]
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Model binding joins repeated headers and turns an empty one into null, which would silently disable idempotency.
        if (Request.Headers.TryGetValue(IdempotencyKeyHeader, out var rawIdempotencyKey)
            && (rawIdempotencyKey.Count != 1 || string.IsNullOrWhiteSpace(rawIdempotencyKey[0])))
        {
            ModelState.AddModelError(IdempotencyKeyHeader, "The Idempotency-Key header must be sent exactly once with a non-empty value.");
            return _apiBehavior.InvalidModelStateResponseFactory(ControllerContext);
        }

        var command = request.ToCommand(idempotencyKey is null ? null : new IdempotencyKey(idempotencyKey));
        var result = await _submitPurchaseOrder.HandleAsync(command, cancellationToken);

        if (result.IdempotentReplay)
        {
            Response.Headers[IdempotentReplayedHeader] = "true";
        }

        return CreatedAtAction(nameof(GetById), new { id = result.PurchaseOrder.Id }, result);
    }

    /// <summary>Returns a purchase order.</summary>
    /// <param name="id">Purchase order id.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <response code="200">The order.</response>
    /// <response code="404">No such order.</response>
    [HttpGet("{id:long:min(1)}")]
    [ProducesResponseType<PurchaseOrderDto>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    public async Task<ActionResult<PurchaseOrderDto>> GetById(long id, CancellationToken cancellationToken) =>
        await _getPurchaseOrder.HandleAsync(new GetPurchaseOrderQuery(id), cancellationToken);

    /// <summary>Returns the shipping slip generated for a purchase order (BR2).</summary>
    /// <param name="id">Purchase order id.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <response code="200">The shipping slip.</response>
    /// <response code="404">No such order, or the order contains no physical products and therefore has no slip.</response>
    [HttpGet("{id:long:min(1)}/shipping-slip")]
    [ProducesResponseType<ShippingSlipDto>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    public async Task<ActionResult<ShippingSlipDto>> GetShippingSlip(long id, CancellationToken cancellationToken) =>
        await _getShippingSlip.HandleAsync(new GetShippingSlipQuery(id), cancellationToken);
}
