using FluentAssertions;
using FunBooksAndVideos.Domain.Common;
using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Domain.Orders;
using FunBooksAndVideos.Domain.ValueObjects;
using FunBooksAndVideos.TestKit;

namespace FunBooksAndVideos.Domain.Tests.Orders;

public sealed class PurchaseOrderTests
{
    private static readonly PurchaseOrderId OrderId = new(3344656);
    private static readonly CustomerId CustomerId = new(4567890);

    [Fact]
    public void Create_builds_a_pending_order_whose_total_is_the_sum_of_its_lines()
    {
        var order = new PurchaseOrderBuilder().WithKataExampleLines().Build();

        order.Id.Should().Be(OrderId);
        order.CustomerId.Should().Be(CustomerId);
        order.Status.Should().Be(PurchaseOrderStatus.Pending);
        order.Total.Should().Be(Money.Of(48.50m));
        order.CreatedAt.Should().Be(TestClock.Now);
        order.ProcessedAt.Should().BeNull();
        order.Version.Should().Be(0);
        order.Lines.Should().HaveCount(3);
        order.Lines.Select(line => line.Description).Should().ContainInOrder(
            "Video \"Comprehensive First Aid Training\"",
            "Book \"The Girl on the train\"",
            "Book Club Membership");
    }

    [Fact]
    public void Create_requires_at_least_one_line()
    {
        var act = () => PurchaseOrder.Create(OrderId, CustomerId, [], TestClock.Now);

        act.Should().Throw<DomainValidationException>().Which.Code.Should().Be("purchase_order.lines.empty");
    }

    [Fact]
    public void Create_rejects_null_lines()
    {
        var nullCollection = () => PurchaseOrder.Create(OrderId, CustomerId, null!, TestClock.Now);
        var nullEntry = () => PurchaseOrder.Create(OrderId, CustomerId, [null!], TestClock.Now);
        var nullAmongValid = () => PurchaseOrder.Create(OrderId, CustomerId, [ProductLine.For(TestProducts.Video()), null!], TestClock.Now);

        nullCollection.Should().Throw<ArgumentNullException>();
        nullEntry.Should().Throw<DomainValidationException>().Which.Code.Should().Be("purchase_order.lines.invalid");
        nullAmongValid.Should().Throw<DomainValidationException>().Which.Code.Should().Be("purchase_order.lines.invalid");
    }

    [Fact]
    public void Create_rejects_a_default_customer_id()
    {
        var act = () => PurchaseOrder.Create(OrderId, default, [ProductLine.For(TestProducts.Book())], TestClock.Now);

        act.Should().Throw<DomainValidationException>().Which.Code.Should().Be("purchase_order.customer_id.invalid");
    }

    [Fact]
    public void Create_rejects_a_default_order_id()
    {
        var act = () => PurchaseOrder.Create(default, CustomerId, [ProductLine.For(TestProducts.Book())], TestClock.Now);

        act.Should().Throw<DomainValidationException>().Which.Code.Should().Be("entity.id.invalid");
    }

    [Theory]
    [InlineData(MembershipType.BookClub, MembershipType.BookClub)]
    [InlineData(MembershipType.VideoClub, MembershipType.VideoClub)]
    [InlineData(MembershipType.Premium, MembershipType.Premium)]
    [InlineData(MembershipType.Premium, MembershipType.BookClub)]
    [InlineData(MembershipType.VideoClub, MembershipType.Premium)]
    public void Create_rejects_memberships_that_overlap(MembershipType first, MembershipType second)
    {
        var act = () => new PurchaseOrderBuilder().WithMembership(first).WithMembership(second).Build();

        act.Should().Throw<DomainValidationException>().Which.Code.Should().Be("purchase_order.memberships.overlap");
    }

    [Fact]
    public void Create_allows_book_club_and_video_club_in_one_order()
    {
        var order = new PurchaseOrderBuilder().WithMembership(MembershipType.BookClub).WithMembership(MembershipType.VideoClub).Build();

        order.MembershipLines.Should().HaveCount(2);
        order.Total.Should().Be(Money.Of(TestPlans.BookClubPrice + TestPlans.VideoClubPrice));
    }

    [Fact]
    public void Create_allows_the_same_product_more_than_once()
    {
        var order = new PurchaseOrderBuilder().WithBook().WithBook().Build();

        order.ProductLines.Should().HaveCount(2);
        order.Total.Should().Be(Money.Of(28.00m));
    }

    [Fact]
    public void Shippable_lines_are_the_physical_product_lines_only()
    {
        var order = new PurchaseOrderBuilder().WithKataExampleLines().Build();

        order.ShippableLines.Should().ContainSingle().Which.ProductName.Should().Be("The Girl on the train");
        order.RequiresShipping.Should().BeTrue();
        order.ContainsMembership.Should().BeTrue();
    }

    [Fact]
    public void Digital_only_orders_need_no_shipping_and_contain_no_membership()
    {
        var order = new PurchaseOrderBuilder().WithVideo().Build();

        order.RequiresShipping.Should().BeFalse();
        order.ContainsMembership.Should().BeFalse();
        order.ShippableLines.Should().BeEmpty();
        order.MembershipLines.Should().BeEmpty();
    }

    [Fact]
    public void Lines_are_copied_on_creation()
    {
        var lines = new List<OrderLine> { ProductLine.For(TestProducts.Video()) };
        var order = PurchaseOrder.Create(OrderId, CustomerId, lines, TestClock.Now);

        lines.Add(ProductLine.For(TestProducts.Book()));

        order.Lines.Should().HaveCount(1);
        order.Lines.Should().NotBeAssignableTo<List<OrderLine>>();
    }

    [Fact]
    public void MarkProcessed_transitions_the_order_once()
    {
        var order = new PurchaseOrderBuilder().Build();
        var processedAt = TestClock.Now.AddSeconds(1);

        order.MarkProcessed(processedAt);

        order.Status.Should().Be(PurchaseOrderStatus.Processed);
        order.ProcessedAt.Should().Be(processedAt);

        var again = () => order.MarkProcessed(processedAt.AddSeconds(1));

        again.Should().Throw<BusinessRuleViolationException>().Which.Code.Should().Be("purchase_order.already_processed");
        order.ProcessedAt.Should().Be(processedAt);
    }

    [Fact]
    public void Rehydrate_restores_a_processed_order()
    {
        var lines = new OrderLine[] { ProductLine.For(TestProducts.Book()) };
        var processedAt = TestClock.Now.AddMinutes(1);

        var order = PurchaseOrder.Rehydrate(OrderId, CustomerId, lines, PurchaseOrderStatus.Processed, TestClock.Now, processedAt, version: 2);

        order.Status.Should().Be(PurchaseOrderStatus.Processed);
        order.ProcessedAt.Should().Be(processedAt);
        order.Version.Should().Be(2);
        order.Total.Should().Be(Money.Of(14.00m));
    }

    [Fact]
    public void Rehydrate_rejects_inconsistent_status_and_processing_time()
    {
        var lines = new OrderLine[] { ProductLine.For(TestProducts.Book()) };

        var processedWithoutTime = () => PurchaseOrder.Rehydrate(OrderId, CustomerId, lines, PurchaseOrderStatus.Processed, TestClock.Now, null, 0);
        var pendingWithTime = () => PurchaseOrder.Rehydrate(OrderId, CustomerId, lines, PurchaseOrderStatus.Pending, TestClock.Now, TestClock.Now, 0);
        var unknownStatus = () => PurchaseOrder.Rehydrate(OrderId, CustomerId, lines, (PurchaseOrderStatus)9, TestClock.Now, null, 0);

        processedWithoutTime.Should().Throw<DomainValidationException>().Which.Code.Should().Be("purchase_order.processed_at.missing");
        pendingWithTime.Should().Throw<DomainValidationException>().Which.Code.Should().Be("purchase_order.processed_at.unexpected");
        unknownStatus.Should().Throw<DomainValidationException>().Which.Code.Should().Be("purchase_order.status.unsupported");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Purchase_order_ids_must_be_positive(long value)
    {
        var act = () => new PurchaseOrderId(value);

        act.Should().Throw<DomainValidationException>().Which.Code.Should().Be("purchase_order_id.invalid");
        new PurchaseOrderId(3344656).ToString().Should().Be("3344656");
    }
}
