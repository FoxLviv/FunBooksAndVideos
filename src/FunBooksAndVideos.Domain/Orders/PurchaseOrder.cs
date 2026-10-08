using System.Collections.ObjectModel;
using FunBooksAndVideos.Domain.Common;
using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Domain.ValueObjects;

namespace FunBooksAndVideos.Domain.Orders;

/// <summary>
/// Purchase order aggregate: a customer, one or more item lines and the total derived from them.
/// </summary>
public sealed class PurchaseOrder : AggregateRoot<PurchaseOrderId>
{
    private readonly ReadOnlyCollection<OrderLine> _lines;

    private PurchaseOrder(
        PurchaseOrderId id,
        CustomerId customerId,
        IEnumerable<OrderLine> lines,
        PurchaseOrderStatus status,
        DateTimeOffset createdAt,
        DateTimeOffset? processedAt,
        int version)
        : base(id, version)
    {
        _lines = lines.ToList().AsReadOnly();
        EnsureLinesAreValid(_lines);

        CustomerId = Guard.NotDefault(customerId, "purchase_order.customer_id.invalid", "Customer id");
        Total = Money.Sum(_lines.Select(line => line.Price));
        ContainsMembership = _lines.Any(line => line is MembershipLine);
        RequiresShipping = _lines.Any(line => line is ProductLine { RequiresShipping: true });
        Status = Guard.DefinedEnum(status, "purchase_order.status.unsupported");
        CreatedAt = createdAt;
        ProcessedAt = processedAt;
    }

    public CustomerId CustomerId { get; }

    public IReadOnlyList<OrderLine> Lines => _lines;

    /// <summary>Sum of all line prices.</summary>
    public Money Total { get; }

    public PurchaseOrderStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset? ProcessedAt { get; private set; }

    public IEnumerable<ProductLine> ProductLines => _lines.OfType<ProductLine>();

    public IEnumerable<MembershipLine> MembershipLines => _lines.OfType<MembershipLine>();

    /// <summary>Product lines that have to be physically delivered.</summary>
    public IEnumerable<ProductLine> ShippableLines => ProductLines.Where(line => line.RequiresShipping);

    public bool ContainsMembership { get; }

    public bool RequiresShipping { get; }

    /// <summary>Creates a new, pending purchase order.</summary>
    public static PurchaseOrder Create(PurchaseOrderId id, CustomerId customerId, IEnumerable<OrderLine> lines, DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(lines);
        return new PurchaseOrder(id, customerId, lines, PurchaseOrderStatus.Pending, createdAt, null, InitialVersion);
    }

    /// <summary>Rebuilds a purchase order from a persisted snapshot. Only the persistence layer should call this.</summary>
    public static PurchaseOrder Rehydrate(
        PurchaseOrderId id,
        CustomerId customerId,
        IEnumerable<OrderLine> lines,
        PurchaseOrderStatus status,
        DateTimeOffset createdAt,
        DateTimeOffset? processedAt,
        int version)
    {
        ArgumentNullException.ThrowIfNull(lines);

        if (status == PurchaseOrderStatus.Processed && processedAt is null)
        {
            throw new DomainValidationException("purchase_order.processed_at.missing", "A processed purchase order must have a processing time.");
        }

        if (status == PurchaseOrderStatus.Pending && processedAt is not null)
        {
            throw new DomainValidationException("purchase_order.processed_at.unexpected", "A pending purchase order cannot have a processing time.");
        }

        return new PurchaseOrder(id, customerId, lines, status, createdAt, processedAt, version);
    }

    /// <summary>Transitions the order to <see cref="PurchaseOrderStatus.Processed"/>. Processing twice is a business rule violation.</summary>
    public void MarkProcessed(DateTimeOffset processedAt)
    {
        if (Status == PurchaseOrderStatus.Processed)
        {
            throw new BusinessRuleViolationException("purchase_order.already_processed", $"Purchase order {Id} has already been processed.");
        }

        Status = PurchaseOrderStatus.Processed;
        ProcessedAt = processedAt;
    }

    private static void EnsureLinesAreValid(ReadOnlyCollection<OrderLine> lines)
    {
        if (lines.Count == 0)
        {
            throw new DomainValidationException("purchase_order.lines.empty", "A purchase order must contain at least one item line.");
        }

        if (lines.Any(line => line is null))
        {
            throw new DomainValidationException("purchase_order.lines.invalid", "Item lines must not contain null entries.");
        }

        var membershipTypes = lines.OfType<MembershipLine>().Select(line => line.MembershipType).ToList();
        for (var i = 0; i < membershipTypes.Count; i++)
        {
            for (var j = i + 1; j < membershipTypes.Count; j++)
            {
                if (membershipTypes[i].OverlapsWith(membershipTypes[j]))
                {
                    throw new DomainValidationException(
                        "purchase_order.memberships.overlap",
                        $"Memberships {membershipTypes[i]} and {membershipTypes[j]} grant access to the same club and cannot be bought in one purchase order.");
                }
            }
        }
    }
}
