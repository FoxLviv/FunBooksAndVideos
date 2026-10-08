using System.Text.Json.Serialization;
using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Domain.Idempotency;

namespace FunBooksAndVideos.Application.PurchaseOrders.SubmitPurchaseOrder;

/// <summary>Submits a purchase order and processes it immediately.</summary>
/// <param name="CustomerId">Customer placing the order.</param>
/// <param name="Lines">Item lines; at least one.</param>
/// <param name="ExpectedTotal">
/// Optional total the client showed to the customer. When present it must match the server-side total,
/// which protects against stale prices on the client.
/// </param>
/// <param name="IdempotencyKey">
/// Optional client-supplied key. Repeating the command with the same key and the same payload replays the stored
/// result instead of creating a second order; the same key with a different payload is rejected.
/// </param>
public sealed record SubmitPurchaseOrderCommand(
    long CustomerId,
    IReadOnlyList<OrderLineRequest> Lines,
    decimal? ExpectedTotal,
    IdempotencyKey? IdempotencyKey = null)
{
    /// <summary>The same command without its idempotency key: the payload a request fingerprint is computed from.</summary>
    public SubmitPurchaseOrderCommand WithoutIdempotencyKey() => this with { IdempotencyKey = null };
}

/// <summary>
/// Requested item line. Closed hierarchy mirroring the domain line types. The JSON discriminator keeps the
/// concrete type visible when the command is serialized (request fingerprints), so a product line and a
/// membership line never look alike.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ProductLineRequest), "Product")]
[JsonDerivedType(typeof(MembershipLineRequest), "Membership")]
public abstract record OrderLineRequest;

/// <summary>A catalog product.</summary>
public sealed record ProductLineRequest(long ProductId) : OrderLineRequest;

/// <summary>A membership.</summary>
public sealed record MembershipLineRequest(MembershipType MembershipType) : OrderLineRequest;
