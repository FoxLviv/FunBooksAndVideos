using FunBooksAndVideos.Domain.Catalog;
using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Domain.Orders;
using FunBooksAndVideos.Domain.ValueObjects;

namespace FunBooksAndVideos.TestKit;

/// <summary>Test data builder (GoF Builder) for pending purchase orders. Without lines it contains one video.</summary>
public sealed class PurchaseOrderBuilder
{
    public const long DefaultId = 3344656;

    private readonly List<OrderLine> _lines = [];
    private long _id = DefaultId;
    private long _customerId = CustomerBuilder.DefaultId;
    private DateTimeOffset _createdAt = TestClock.Now;

    public PurchaseOrderBuilder WithId(long id)
    {
        _id = id;
        return this;
    }

    public PurchaseOrderBuilder ForCustomer(long customerId)
    {
        _customerId = customerId;
        return this;
    }

    public PurchaseOrderBuilder ForCustomer(Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);
        return ForCustomer(customer.Id.Value);
    }

    public PurchaseOrderBuilder CreatedAt(DateTimeOffset createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    public PurchaseOrderBuilder WithLine(OrderLine line)
    {
        _lines.Add(line);
        return this;
    }

    public PurchaseOrderBuilder WithProduct(Product product) => WithLine(ProductLine.For(product));

    public PurchaseOrderBuilder WithBook(long id = TestProducts.DefaultBookId) => WithProduct(TestProducts.Book(id));

    public PurchaseOrderBuilder WithVideo(long id = TestProducts.DefaultVideoId) => WithProduct(TestProducts.Video(id));

    public PurchaseOrderBuilder WithMembership(MembershipType type) => WithLine(MembershipLine.For(TestPlans.For(type)));

    public PurchaseOrderBuilder WithMembership(MembershipType type, decimal price) => WithLine(new MembershipLine(type, Money.Of(price)));

    /// <summary>Video, book and book club membership: the example from the kata (total 48.50).</summary>
    public PurchaseOrderBuilder WithKataExampleLines() => WithVideo().WithBook().WithMembership(MembershipType.BookClub);

    public PurchaseOrder Build()
    {
        var lines = _lines.Count == 0 ? [ProductLine.For(TestProducts.Video())] : _lines;
        return PurchaseOrder.Create(new PurchaseOrderId(_id), new CustomerId(_customerId), lines, _createdAt);
    }
}
