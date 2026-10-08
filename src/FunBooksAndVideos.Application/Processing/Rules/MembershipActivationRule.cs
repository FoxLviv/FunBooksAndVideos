using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Domain.Orders;
using FunBooksAndVideos.Domain.Persistence;
using Microsoft.Extensions.Logging;

namespace FunBooksAndVideos.Application.Processing.Rules;

/// <summary>
/// BR1: if the purchase order contains a membership, it has to be activated in the customer account immediately.
/// Activation is idempotent: a membership the customer already has is reported as already active.
/// </summary>
public sealed partial class MembershipActivationRule : IPurchaseOrderRule
{
    public const string RuleName = "BR1.MembershipActivation";

    private readonly ICustomerRepository _customers;
    private readonly ILogger<MembershipActivationRule> _logger;

    public MembershipActivationRule(ICustomerRepository customers, ILogger<MembershipActivationRule> logger)
    {
        ArgumentNullException.ThrowIfNull(customers);
        ArgumentNullException.ThrowIfNull(logger);

        _customers = customers;
        _logger = logger;
    }

    public string Name => RuleName;

    public bool AppliesTo(PurchaseOrder order)
    {
        ArgumentNullException.ThrowIfNull(order);
        return order.ContainsMembership;
    }

    public Task ApplyAsync(PurchaseOrderProcessingContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var accountChanged = false;
        foreach (var line in context.Order.MembershipLines)
        {
            var result = context.Customer.ActivateMembership(line.MembershipType, context.ProcessedAt);
            context.AddEffect(new MembershipActivationEffect(Name, result));
            accountChanged |= result.Outcome == MembershipActivationOutcome.Activated;

            LogMembershipProcessed(_logger, line.MembershipType, context.Customer.Id, result.Outcome);
        }

        // Only stage a write when something changed: no pointless version bump, no spurious concurrency conflicts.
        if (accountChanged)
        {
            _customers.Update(context.Customer);
        }

        return Task.CompletedTask;
    }

    [LoggerMessage(EventId = 1100, Level = LogLevel.Debug, Message = "Membership {MembershipType} for customer {CustomerId}: {Outcome}.")]
    private static partial void LogMembershipProcessed(ILogger logger, MembershipType membershipType, CustomerId customerId, MembershipActivationOutcome outcome);
}
