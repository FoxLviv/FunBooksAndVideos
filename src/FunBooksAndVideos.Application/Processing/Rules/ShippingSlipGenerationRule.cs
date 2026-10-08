using FunBooksAndVideos.Domain.Orders;
using FunBooksAndVideos.Domain.Persistence;
using FunBooksAndVideos.Domain.Shipping;
using Microsoft.Extensions.Logging;

namespace FunBooksAndVideos.Application.Processing.Rules;

/// <summary>
/// BR2: if the purchase order contains a physical product, a shipping slip has to be generated.
/// Only the physical product lines end up on the slip; a customer without a shipping address makes the
/// whole processing fail (nothing is committed), because the order could not be fulfilled.
/// </summary>
public sealed partial class ShippingSlipGenerationRule : IPurchaseOrderRule
{
    public const string RuleName = "BR2.ShippingSlipGeneration";

    private readonly IShippingSlipRepository _shippingSlips;
    private readonly ILogger<ShippingSlipGenerationRule> _logger;

    public ShippingSlipGenerationRule(IShippingSlipRepository shippingSlips, ILogger<ShippingSlipGenerationRule> logger)
    {
        ArgumentNullException.ThrowIfNull(shippingSlips);
        ArgumentNullException.ThrowIfNull(logger);

        _shippingSlips = shippingSlips;
        _logger = logger;
    }

    public string Name => RuleName;

    public bool AppliesTo(PurchaseOrder order)
    {
        ArgumentNullException.ThrowIfNull(order);
        return order.RequiresShipping;
    }

    public async Task ApplyAsync(PurchaseOrderProcessingContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var slipId = await _shippingSlips.NextIdentityAsync(cancellationToken);
        var slip = ShippingSlipFactory.CreateFor(slipId, context.Order, context.Customer, context.ProcessedAt);

        _shippingSlips.Add(slip);
        context.AddEffect(new ShippingSlipGeneratedEffect(Name, slip));

        LogSlipGenerated(_logger, slip.Id, context.Order.Id, slip.Items.Count);
    }

    [LoggerMessage(EventId = 1200, Level = LogLevel.Debug, Message = "Shipping slip {ShippingSlipId} generated for purchase order {PurchaseOrderId} with {ItemCount} item(s).")]
    private static partial void LogSlipGenerated(ILogger logger, ShippingSlipId shippingSlipId, PurchaseOrderId purchaseOrderId, int itemCount);
}
