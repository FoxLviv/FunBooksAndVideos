namespace FunBooksAndVideos.Domain.Orders;

/// <summary>Lifecycle of a purchase order.</summary>
public enum PurchaseOrderStatus
{
    /// <summary>Created, business rules not applied yet.</summary>
    Pending = 1,

    /// <summary>All business rules applied.</summary>
    Processed = 2,
}
