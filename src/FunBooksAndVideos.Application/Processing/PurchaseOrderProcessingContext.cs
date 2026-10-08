using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Domain.Orders;

namespace FunBooksAndVideos.Application.Processing;

/// <summary>
/// Everything a rule needs while an order is processed, plus the place where rules record what they did.
/// One context is created per processing run; it is not shared between orders.
/// </summary>
public sealed class PurchaseOrderProcessingContext
{
    private readonly List<ProcessingEffect> _effects = [];
    private readonly List<string> _appliedRules = [];

    public PurchaseOrderProcessingContext(PurchaseOrder order, Customer customer, DateTimeOffset processedAt)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(customer);

        if (order.CustomerId != customer.Id)
        {
            throw new ArgumentException(
                $"Purchase order {order.Id} belongs to customer {order.CustomerId}, but customer {customer.Id} was supplied.",
                nameof(customer));
        }

        Order = order;
        Customer = customer;
        ProcessedAt = processedAt;
    }

    public PurchaseOrder Order { get; }

    /// <summary>The account the order belongs to. Rules mutate this instance; the caller persists it.</summary>
    public Customer Customer { get; }

    /// <summary>Single timestamp shared by all rules of one run, so that every artefact carries the same time.</summary>
    public DateTimeOffset ProcessedAt { get; }

    public IReadOnlyList<ProcessingEffect> Effects => _effects.AsReadOnly();

    /// <summary>Names of the rules that applied, in execution order.</summary>
    public IReadOnlyList<string> AppliedRules => _appliedRules.AsReadOnly();

    public void AddEffect(ProcessingEffect effect)
    {
        ArgumentNullException.ThrowIfNull(effect);
        _effects.Add(effect);
    }

    internal void MarkRuleApplied(string ruleName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ruleName);
        _appliedRules.Add(ruleName);
    }
}
