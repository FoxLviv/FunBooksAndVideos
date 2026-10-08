using FunBooksAndVideos.Application.Dtos;

namespace FunBooksAndVideos.Application.PurchaseOrders.SubmitPurchaseOrder;

/// <summary>Result of submitting and processing a purchase order.</summary>
/// <param name="PurchaseOrder">The persisted, processed order.</param>
/// <param name="MembershipActivations">Outcome of every membership line (BR1).</param>
/// <param name="ShippingSlip">Shipping slip for the physical products (BR2); <see langword="null"/> when nothing has to be shipped.</param>
/// <param name="AppliedRules">Business rules that applied, in execution order.</param>
public sealed record SubmitPurchaseOrderResult(
    PurchaseOrderDto PurchaseOrder,
    IReadOnlyList<MembershipActivationDto> MembershipActivations,
    ShippingSlipDto? ShippingSlip,
    IReadOnlyList<string> AppliedRules)
{
    /// <summary>
    /// <see langword="true"/> when this result was not produced now but replayed from an earlier request with the same
    /// idempotency key and payload; nothing was created or changed by the current request. Not serialized: a replayed
    /// response body is byte-for-byte the original one, the replay is signalled by the <c>Idempotent-Replayed</c> header.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IdempotentReplay { get; init; }
}
