using FunBooksAndVideos.Domain.ValueObjects;

namespace FunBooksAndVideos.Domain.Orders;

/// <summary>
/// One item line of a purchase order. Lines are immutable snapshots: later catalog changes never
/// alter what the customer actually bought.
/// </summary>
public abstract class OrderLine
{
    protected OrderLine(Money price)
    {
        Price = price;
    }

    public Money Price { get; }

    /// <summary>Human readable description, e.g. <c>Book "The Girl on the train"</c>.</summary>
    public abstract string Description { get; }

    public abstract TResult Accept<TResult>(IOrderLineVisitor<TResult> visitor);

    public override string ToString() => $"{Description} ({Price})";
}
