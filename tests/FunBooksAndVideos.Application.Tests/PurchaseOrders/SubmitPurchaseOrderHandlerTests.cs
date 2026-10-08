using System.Text.Json;
using FluentAssertions;
using FunBooksAndVideos.Application.Exceptions;
using FunBooksAndVideos.Application.Idempotency;
using FunBooksAndVideos.Application.Processing;
using FunBooksAndVideos.Application.PurchaseOrders.SubmitPurchaseOrder;
using FunBooksAndVideos.Domain.Common;
using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Domain.Idempotency;
using FunBooksAndVideos.Domain.Orders;
using FunBooksAndVideos.Domain.Persistence;
using FunBooksAndVideos.Domain.Shipping;
using FunBooksAndVideos.TestKit;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace FunBooksAndVideos.Application.Tests.PurchaseOrders;

public sealed class SubmitPurchaseOrderHandlerTests
{
    private static readonly PurchaseOrderId NextOrderId = new(3344656);

    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly IPurchaseOrderRepository _orders = Substitute.For<IPurchaseOrderRepository>();
    private readonly IOrderLineFactory _lineFactory = Substitute.For<IOrderLineFactory>();
    private readonly IPurchaseOrderProcessor _processor = Substitute.For<IPurchaseOrderProcessor>();
    private readonly IIdempotencyRecordRepository _idempotencyRecords = Substitute.For<IIdempotencyRecordRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly Customer _customer = new CustomerBuilder().Build();
    private readonly SubmitPurchaseOrderHandler _handler;

    private readonly IReadOnlyList<OrderLineRequest> _requests =
    [
        new ProductLineRequest(TestProducts.DefaultVideoId),
        new ProductLineRequest(TestProducts.DefaultBookId),
        new MembershipLineRequest(MembershipType.BookClub),
    ];

    private readonly IReadOnlyList<OrderLine> _lines =
    [
        ProductLine.For(TestProducts.Video()),
        ProductLine.For(TestProducts.Book()),
        MembershipLine.For(TestPlans.BookClub()),
    ];

    public SubmitPurchaseOrderHandlerTests()
    {
        _customers.FindAsync(_customer.Id, Arg.Any<CancellationToken>()).Returns(_customer);
        _orders.NextIdentityAsync(Arg.Any<CancellationToken>()).Returns(NextOrderId);
        _lineFactory.CreateAsync(_requests, Arg.Any<CancellationToken>()).Returns(_lines);
        _processor.ProcessAsync(Arg.Any<PurchaseOrder>(), _customer, Arg.Any<CancellationToken>())
            .Returns(call => ProcessLikeTheRealThing(call.Arg<PurchaseOrder>(), _customer));

        _handler = new SubmitPurchaseOrderHandler(
            _customers, _orders, _lineFactory, _processor, _idempotencyRecords, _unitOfWork, TestClock.Create(), NullLogger<SubmitPurchaseOrderHandler>.Instance);
    }

    private SubmitPurchaseOrderCommand Command(decimal? expectedTotal = null, IdempotencyKey? key = null) =>
        new(_customer.Id.Value, _requests, expectedTotal, key);

    private static PurchaseOrderProcessingResult ProcessLikeTheRealThing(PurchaseOrder order, Customer customer)
    {
        var activation = customer.ActivateMembership(MembershipType.BookClub, TestClock.Now);
        var slip = ShippingSlipFactory.CreateFor(new ShippingSlipId(5), order, customer, TestClock.Now);
        order.MarkProcessed(TestClock.Now);

        return new PurchaseOrderProcessingResult(
            order,
            [new MembershipActivationEffect("BR1", activation), new ShippingSlipGeneratedEffect("BR2", slip)],
            ["BR1", "BR2"]);
    }

    [Fact]
    public async Task Builds_processes_and_persists_the_order_in_one_unit_of_work()
    {
        var result = await _handler.HandleAsync(Command(), CancellationToken.None);

        result.PurchaseOrder.Id.Should().Be(NextOrderId.Value);
        result.PurchaseOrder.CustomerId.Should().Be(_customer.Id.Value);
        result.PurchaseOrder.Total.Should().Be(48.50m);
        result.PurchaseOrder.Status.Should().Be(PurchaseOrderStatus.Processed);
        result.PurchaseOrder.CreatedAt.Should().Be(TestClock.Now);
        result.PurchaseOrder.Lines.Should().HaveCount(3);
        result.MembershipActivations.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            MembershipType = MembershipType.BookClub,
            Outcome = MembershipActivationOutcome.Activated,
            ActivatedAt = TestClock.Now,
        });
        result.ShippingSlip.Should().NotBeNull();
        result.ShippingSlip!.Id.Should().Be(5);
        result.ShippingSlip.Items.Should().ContainSingle().Which.ProductId.Should().Be(TestProducts.DefaultBookId);
        result.AppliedRules.Should().Equal("BR1", "BR2");

        Received.InOrder(() =>
        {
            _processor.ProcessAsync(Arg.Is<PurchaseOrder>(order => order.Id == NextOrderId), _customer, Arg.Any<CancellationToken>());
            _orders.Add(Arg.Is<PurchaseOrder>(order => order.Id == NextOrderId && order.Status == PurchaseOrderStatus.Processed));
            _unitOfWork.CommitAsync(Arg.Any<CancellationToken>());
        });
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task With_an_idempotency_key_the_record_is_staged_before_the_commit()
    {
        var key = new IdempotencyKey("order-42");
        var command = Command(key: key);

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        var expectedFingerprint = RequestFingerprint.Compute(command.WithoutIdempotencyKey());
        Received.InOrder(() =>
        {
            _orders.Add(Arg.Any<PurchaseOrder>());
            _idempotencyRecords.Add(Arg.Is<IdempotencyRecord>(record =>
                record.Key == key && record.RequestFingerprint == expectedFingerprint && record.CreatedAt == TestClock.Now));
            _unitOfWork.CommitAsync(Arg.Any<CancellationToken>());
        });

        var stored = _idempotencyRecords.ReceivedCalls().Single().GetArguments()[0].Should().BeOfType<IdempotencyRecord>().Subject;
        var replayed = JsonSerializer.Deserialize<SubmitPurchaseOrderResult>(stored.ResponsePayload, IdempotencyJson.Options);
        replayed.Should().BeEquivalentTo(result);
        replayed!.IdempotentReplay.Should().BeFalse();
    }

    [Fact]
    public async Task Without_an_idempotency_key_nothing_is_staged()
    {
        await _handler.HandleAsync(Command(), CancellationToken.None);

        _idempotencyRecords.DidNotReceive().Add(Arg.Any<IdempotencyRecord>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_matching_expected_total_is_accepted()
    {
        var result = await _handler.HandleAsync(Command(expectedTotal: 48.50m), CancellationToken.None);

        result.PurchaseOrder.Total.Should().Be(48.50m);
    }

    [Fact]
    public async Task A_mismatching_expected_total_is_rejected_before_processing()
    {
        var act = () => _handler.HandleAsync(Command(expectedTotal: 48.51m), CancellationToken.None);

        var exception = (await act.Should().ThrowAsync<ValidationException>()).Which;
        exception.Errors.Should().ContainKey("expectedTotal").WhoseValue.Should().ContainSingle().Which.Should().Contain("48.51").And.Contain("48.50");
        await _processor.DidNotReceive().ProcessAsync(Arg.Any<PurchaseOrder>(), Arg.Any<Customer>(), Arg.Any<CancellationToken>());
        _orders.DidNotReceive().Add(Arg.Any<PurchaseOrder>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(48.505)]
    public async Task An_invalid_expected_total_is_a_validation_error_on_that_field(decimal expectedTotal)
    {
        var act = () => _handler.HandleAsync(Command(expectedTotal), CancellationToken.None);

        var exception = (await act.Should().ThrowAsync<ValidationException>()).Which;
        exception.Errors.Should().ContainKey("expectedTotal");
        await _lineFactory.DidNotReceive().CreateAsync(Arg.Any<IReadOnlyList<OrderLineRequest>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task An_unknown_customer_is_a_validation_error_and_nothing_is_touched()
    {
        _customers.FindAsync(Arg.Any<CustomerId>(), Arg.Any<CancellationToken>()).ReturnsNull();

        var act = () => _handler.HandleAsync(Command(), CancellationToken.None);

        var exception = (await act.Should().ThrowAsync<ValidationException>()).Which;
        exception.Errors.Should().ContainKey("customerId");
        await _lineFactory.DidNotReceive().CreateAsync(Arg.Any<IReadOnlyList<OrderLineRequest>>(), Arg.Any<CancellationToken>());
        await _orders.DidNotReceive().NextIdentityAsync(Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Line_resolution_errors_propagate_without_allocating_an_order_id()
    {
        _lineFactory.CreateAsync(_requests, Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<OrderLine>>(_ => throw ValidationException.For("lines[0].productId", "Product 1 does not exist."));

        var act = () => _handler.HandleAsync(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        await _orders.DidNotReceive().NextIdentityAsync(Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_failing_business_rule_prevents_persistence()
    {
        _processor.ProcessAsync(Arg.Any<PurchaseOrder>(), _customer, Arg.Any<CancellationToken>())
            .Returns<PurchaseOrderProcessingResult>(_ => throw new BusinessRuleViolationException("customer.shipping_address.missing", "no address"));

        var act = () => _handler.HandleAsync(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleViolationException>();
        _orders.DidNotReceive().Add(Arg.Any<PurchaseOrder>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Commit_failures_are_not_swallowed()
    {
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>())
            .Returns(_ => throw new ConcurrencyConflictException("Customer", _customer.Id.Value));

        var act = () => _handler.HandleAsync(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<ConcurrencyConflictException>();
    }

    [Fact]
    public async Task Orders_without_effects_map_to_an_empty_result()
    {
        _processor.ProcessAsync(Arg.Any<PurchaseOrder>(), _customer, Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var order = call.Arg<PurchaseOrder>();
                order.MarkProcessed(TestClock.Now);
                return new PurchaseOrderProcessingResult(order, [], []);
            });

        var result = await _handler.HandleAsync(Command(), CancellationToken.None);

        result.MembershipActivations.Should().BeEmpty();
        result.ShippingSlip.Should().BeNull();
        result.AppliedRules.Should().BeEmpty();
    }

    [Fact]
    public void Rejects_null_dependencies()
    {
        var clock = TestClock.Create();
        var logger = NullLogger<SubmitPurchaseOrderHandler>.Instance;
        var constructions = new Func<SubmitPurchaseOrderHandler>[]
        {
            () => new SubmitPurchaseOrderHandler(null!, _orders, _lineFactory, _processor, _idempotencyRecords, _unitOfWork, clock, logger),
            () => new SubmitPurchaseOrderHandler(_customers, null!, _lineFactory, _processor, _idempotencyRecords, _unitOfWork, clock, logger),
            () => new SubmitPurchaseOrderHandler(_customers, _orders, null!, _processor, _idempotencyRecords, _unitOfWork, clock, logger),
            () => new SubmitPurchaseOrderHandler(_customers, _orders, _lineFactory, null!, _idempotencyRecords, _unitOfWork, clock, logger),
            () => new SubmitPurchaseOrderHandler(_customers, _orders, _lineFactory, _processor, null!, _unitOfWork, clock, logger),
            () => new SubmitPurchaseOrderHandler(_customers, _orders, _lineFactory, _processor, _idempotencyRecords, null!, clock, logger),
            () => new SubmitPurchaseOrderHandler(_customers, _orders, _lineFactory, _processor, _idempotencyRecords, _unitOfWork, null!, logger),
            () => new SubmitPurchaseOrderHandler(_customers, _orders, _lineFactory, _processor, _idempotencyRecords, _unitOfWork, clock, null!),
        };

        foreach (var construct in constructions)
        {
            construct.Should().Throw<ArgumentNullException>();
        }
    }

    [Fact]
    public async Task Rejects_a_null_command()
    {
        var act = () => _handler.HandleAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
