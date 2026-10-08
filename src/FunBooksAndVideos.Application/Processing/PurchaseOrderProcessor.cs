using FunBooksAndVideos.Domain.Common;
using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Domain.Orders;
using Microsoft.Extensions.Logging;

namespace FunBooksAndVideos.Application.Processing;

/// <summary>
/// Runs the registered <see cref="IPurchaseOrderRule"/>s in registration order against a pending order.
/// The processor knows nothing about individual rules: adding, removing or re-ordering rules is a
/// composition-root concern. Rules only stage changes; nothing is committed here, so when a rule fails
/// the caller simply never commits and the request scope is discarded.
/// </summary>
public sealed partial class PurchaseOrderProcessor : IPurchaseOrderProcessor
{
    private readonly IReadOnlyList<IPurchaseOrderRule> _rules;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<PurchaseOrderProcessor> _logger;

    public PurchaseOrderProcessor(IEnumerable<IPurchaseOrderRule> rules, TimeProvider timeProvider, ILogger<PurchaseOrderProcessor> logger)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _rules = rules.ToList();
        if (_rules.Any(rule => rule is null))
        {
            throw new ArgumentException("Rules must not contain null entries.", nameof(rules));
        }

        var duplicate = _rules
            .GroupBy(rule => rule.Name, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new ArgumentException($"Rule name '{duplicate.Key}' is registered more than once.", nameof(rules));
        }

        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <summary>Names of the rules in execution order.</summary>
    public IReadOnlyList<string> RuleNames => _rules.Select(rule => rule.Name).ToList();

    public async Task<PurchaseOrderProcessingResult> ProcessAsync(PurchaseOrder order, Customer customer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(customer);

        if (order.Status != PurchaseOrderStatus.Pending)
        {
            throw new BusinessRuleViolationException(
                "purchase_order.already_processed",
                $"Purchase order {order.Id} has already been processed.");
        }

        var context = new PurchaseOrderProcessingContext(order, customer, _timeProvider.GetUtcNow());

        foreach (var rule in _rules)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!rule.AppliesTo(order))
            {
                LogRuleSkipped(_logger, rule.Name, order.Id);
                continue;
            }

            await rule.ApplyAsync(context, cancellationToken);
            context.MarkRuleApplied(rule.Name);
            LogRuleApplied(_logger, rule.Name, order.Id);
        }

        order.MarkProcessed(context.ProcessedAt);
        LogOrderProcessed(_logger, order.Id, context.AppliedRules.Count);

        return new PurchaseOrderProcessingResult(order, context.Effects, context.AppliedRules);
    }

    [LoggerMessage(EventId = 1000, Level = LogLevel.Debug, Message = "Rule {RuleName} does not apply to purchase order {PurchaseOrderId}; skipped.")]
    private static partial void LogRuleSkipped(ILogger logger, string ruleName, PurchaseOrderId purchaseOrderId);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Debug, Message = "Rule {RuleName} applied to purchase order {PurchaseOrderId}.")]
    private static partial void LogRuleApplied(ILogger logger, string ruleName, PurchaseOrderId purchaseOrderId);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Debug, Message = "Purchase order {PurchaseOrderId} processed (not yet committed); {AppliedRuleCount} rule(s) applied.")]
    private static partial void LogOrderProcessed(ILogger logger, PurchaseOrderId purchaseOrderId, int appliedRuleCount);
}
