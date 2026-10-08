using FunBooksAndVideos.Domain.Orders;

namespace FunBooksAndVideos.Application.Processing;

/// <summary>Outcome of one processing run.</summary>
public sealed class PurchaseOrderProcessingResult
{
    public PurchaseOrderProcessingResult(PurchaseOrder order, IReadOnlyList<ProcessingEffect> effects, IReadOnlyList<string> appliedRules)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(effects);
        ArgumentNullException.ThrowIfNull(appliedRules);

        Order = order;
        Effects = effects;
        AppliedRules = appliedRules;
    }

    public PurchaseOrder Order { get; }

    public IReadOnlyList<ProcessingEffect> Effects { get; }

    public IReadOnlyList<string> AppliedRules { get; }

    public IEnumerable<TEffect> EffectsOf<TEffect>()
        where TEffect : ProcessingEffect =>
        Effects.OfType<TEffect>();
}
