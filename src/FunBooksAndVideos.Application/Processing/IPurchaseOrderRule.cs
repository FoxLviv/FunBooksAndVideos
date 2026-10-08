using FunBooksAndVideos.Domain.Orders;

namespace FunBooksAndVideos.Application.Processing;

/// <summary>
/// One business rule applied while a purchase order is processed (GoF Strategy, composed into a
/// Chain by <see cref="PurchaseOrderProcessor"/>). New rules are added by implementing this interface
/// and registering it; the processor itself never changes (Open/Closed).
/// </summary>
public interface IPurchaseOrderRule
{
    /// <summary>Unique, stable name reported back to callers, e.g. <c>BR1.MembershipActivation</c>.</summary>
    string Name { get; }

    /// <summary>Cheap, side-effect free check whether the rule is relevant for the order.</summary>
    bool AppliesTo(PurchaseOrder order);

    /// <summary>Applies the rule. Changes are staged through repositories and committed by the caller.</summary>
    Task ApplyAsync(PurchaseOrderProcessingContext context, CancellationToken cancellationToken);
}
