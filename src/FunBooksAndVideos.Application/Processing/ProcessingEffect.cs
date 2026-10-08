using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Domain.Shipping;

namespace FunBooksAndVideos.Application.Processing;

/// <summary>
/// Something a rule did while processing an order. Rules append effects to the
/// <see cref="PurchaseOrderProcessingContext"/>; callers pick the ones they care about by type.
/// </summary>
public abstract record ProcessingEffect
{
    protected ProcessingEffect(string ruleName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ruleName);
        RuleName = ruleName;
    }

    public string RuleName { get; }

    public abstract string Description { get; }
}

/// <summary>A membership line was applied to the customer account (BR1).</summary>
public sealed record MembershipActivationEffect : ProcessingEffect
{
    public MembershipActivationEffect(string ruleName, MembershipActivationResult result)
        : base(ruleName)
    {
        ArgumentNullException.ThrowIfNull(result);
        Result = result;
    }

    public MembershipActivationResult Result { get; }

    public override string Description =>
        Result.Outcome switch
        {
            MembershipActivationOutcome.Activated => $"{Result.Type.GetDisplayName()} activated.",
            MembershipActivationOutcome.AlreadyActive => $"{Result.Type.GetDisplayName()} was already active; the account was not changed.",
            _ => $"{Result.Type.GetDisplayName()}: {Result.Outcome}.",
        };
}

/// <summary>A shipping slip was generated for the physical products of the order (BR2).</summary>
public sealed record ShippingSlipGeneratedEffect : ProcessingEffect
{
    public ShippingSlipGeneratedEffect(string ruleName, ShippingSlip shippingSlip)
        : base(ruleName)
    {
        ArgumentNullException.ThrowIfNull(shippingSlip);
        ShippingSlip = shippingSlip;
    }

    public ShippingSlip ShippingSlip { get; }

    public override string Description =>
        $"Shipping slip {ShippingSlip.Id} generated with {ShippingSlip.Items.Count} item(s).";
}
