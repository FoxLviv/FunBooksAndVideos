using System.Text.Json;
using FluentAssertions;
using FunBooksAndVideos.Application.Abstractions;
using FunBooksAndVideos.Application.Dtos;
using FunBooksAndVideos.Application.Idempotency;
using FunBooksAndVideos.Application.PurchaseOrders.SubmitPurchaseOrder;
using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Domain.Idempotency;
using FunBooksAndVideos.Domain.Orders;
using FunBooksAndVideos.Domain.Persistence;
using FunBooksAndVideos.TestKit;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace FunBooksAndVideos.Application.Tests.Idempotency;

public sealed class IdempotentSubmitPurchaseOrderDecoratorTests
{
    private static readonly IdempotencyKey Key = new("order-42");

    private readonly ICommandHandler<SubmitPurchaseOrderCommand, SubmitPurchaseOrderResult> _inner =
        Substitute.For<ICommandHandler<SubmitPurchaseOrderCommand, SubmitPurchaseOrderResult>>();

    private readonly IIdempotencyRecordRepository _records = Substitute.For<IIdempotencyRecordRepository>();
    private readonly IdempotentSubmitPurchaseOrderDecorator _decorator;

    public IdempotentSubmitPurchaseOrderDecoratorTests()
    {
        _decorator = new IdempotentSubmitPurchaseOrderDecorator(_inner, _records, NullLogger<IdempotentSubmitPurchaseOrderDecorator>.Instance);
    }

    private static SubmitPurchaseOrderCommand Command(IdempotencyKey? key = null, long customerId = 4567890) =>
        new(customerId, [new MembershipLineRequest(MembershipType.BookClub)], null, key);

    private static SubmitPurchaseOrderResult Result(long orderId = 3344656) =>
        new(
            new PurchaseOrderDto(
                orderId,
                4567890,
                15.00m,
                PurchaseOrderStatus.Processed,
                [new OrderLineDto(OrderLineType.Membership, "Book Club membership", 15.00m, null, null, null, MembershipType.BookClub)],
                TestClock.Now,
                TestClock.Now),
            [new MembershipActivationDto(MembershipType.BookClub, MembershipActivationOutcome.Activated, TestClock.Now)],
            null,
            ["BR1"]);

    private static IdempotencyRecord StoredFor(SubmitPurchaseOrderCommand command, SubmitPurchaseOrderResult result) =>
        new(Key, RequestFingerprint.Compute(command.WithoutIdempotencyKey()), JsonSerializer.Serialize(result, IdempotencyJson.Options), TestClock.Now);

    [Fact]
    public async Task Commands_without_a_key_pass_straight_through()
    {
        var command = Command();
        var result = Result();
        _inner.HandleAsync(command, Arg.Any<CancellationToken>()).Returns(result);

        var actual = await _decorator.HandleAsync(command, CancellationToken.None);

        actual.Should().BeSameAs(result);
        await _records.DidNotReceive().FindAsync(Arg.Any<IdempotencyKey>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_new_key_executes_the_command()
    {
        var command = Command(Key);
        var result = Result();
        _records.FindAsync(Key, Arg.Any<CancellationToken>()).ReturnsNull();
        _inner.HandleAsync(command, Arg.Any<CancellationToken>()).Returns(result);

        var actual = await _decorator.HandleAsync(command, CancellationToken.None);

        actual.Should().BeSameAs(result);
        actual.IdempotentReplay.Should().BeFalse();
    }

    [Fact]
    public async Task A_known_key_with_the_same_payload_replays_the_stored_result_without_executing_the_command()
    {
        var command = Command(Key);
        var original = Result();
        _records.FindAsync(Key, Arg.Any<CancellationToken>()).Returns(StoredFor(command, original));

        var actual = await _decorator.HandleAsync(command, CancellationToken.None);

        actual.IdempotentReplay.Should().BeTrue();
        actual.Should().BeEquivalentTo(original, options => options.Excluding(result => result.IdempotentReplay));
        await _inner.DidNotReceive().HandleAsync(Arg.Any<SubmitPurchaseOrderCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_known_key_with_a_different_payload_is_rejected()
    {
        _records.FindAsync(Key, Arg.Any<CancellationToken>()).Returns(StoredFor(Command(Key, customerId: 1), Result()));

        var act = () => _decorator.HandleAsync(Command(Key, customerId: 2), CancellationToken.None);

        var exception = (await act.Should().ThrowAsync<IdempotencyPayloadMismatchException>()).Which;
        exception.Code.Should().Be("idempotency.payload_mismatch");
        exception.Message.Should().Be("Idempotency key 'order-42' was already used with a different request payload.");
        await _inner.DidNotReceive().HandleAsync(Arg.Any<SubmitPurchaseOrderCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Losing_a_race_on_the_same_key_replays_the_winners_result()
    {
        var command = Command(Key);
        var winner = Result(orderId: 7);
        _records.FindAsync(Key, Arg.Any<CancellationToken>()).Returns(null, StoredFor(command, winner));
        _inner.HandleAsync(command, Arg.Any<CancellationToken>())
            .Returns<SubmitPurchaseOrderResult>(_ => throw new DuplicateEntityException(IdempotencyRecord.EntityName, Key.Value));

        var actual = await _decorator.HandleAsync(command, CancellationToken.None);

        actual.IdempotentReplay.Should().BeTrue();
        actual.PurchaseOrder.Id.Should().Be(7);
        await _records.Received(2).FindAsync(Key, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Losing_a_race_against_a_different_payload_is_a_mismatch()
    {
        var command = Command(Key, customerId: 2);
        _records.FindAsync(Key, Arg.Any<CancellationToken>()).Returns(null, StoredFor(Command(Key, customerId: 1), Result()));
        _inner.HandleAsync(command, Arg.Any<CancellationToken>())
            .Returns<SubmitPurchaseOrderResult>(_ => throw new DuplicateEntityException(IdempotencyRecord.EntityName, Key.Value));

        var act = () => _decorator.HandleAsync(command, CancellationToken.None);

        await act.Should().ThrowAsync<IdempotencyPayloadMismatchException>();
    }

    [Fact]
    public async Task A_duplicate_key_that_cannot_be_found_afterwards_is_rethrown()
    {
        var command = Command(Key);
        _records.FindAsync(Key, Arg.Any<CancellationToken>()).ReturnsNull();
        _inner.HandleAsync(command, Arg.Any<CancellationToken>())
            .Returns<SubmitPurchaseOrderResult>(_ => throw new DuplicateEntityException(IdempotencyRecord.EntityName, Key.Value));

        var act = () => _decorator.HandleAsync(command, CancellationToken.None);

        await act.Should().ThrowAsync<DuplicateEntityException>();
    }

    [Fact]
    public async Task Other_duplicates_and_failures_of_the_inner_handler_propagate()
    {
        var command = Command(Key);
        _records.FindAsync(Key, Arg.Any<CancellationToken>()).ReturnsNull();
        _inner.HandleAsync(command, Arg.Any<CancellationToken>()).Returns<SubmitPurchaseOrderResult>(
            _ => throw new DuplicateEntityException("Purchase order", 1L),
            _ => throw new ConcurrencyConflictException("Customer", 1L));

        var duplicate = () => _decorator.HandleAsync(command, CancellationToken.None);
        await duplicate.Should().ThrowAsync<DuplicateEntityException>().Where(ex => ex.EntityName == "Purchase order");

        var conflict = () => _decorator.HandleAsync(command, CancellationToken.None);
        await conflict.Should().ThrowAsync<ConcurrencyConflictException>();

        await _records.Received(2).FindAsync(Key, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Validates_its_arguments()
    {
        var logger = NullLogger<IdempotentSubmitPurchaseOrderDecorator>.Instance;

        ((Action)(() => _ = new IdempotentSubmitPurchaseOrderDecorator(null!, _records, logger))).Should().Throw<ArgumentNullException>();
        ((Action)(() => _ = new IdempotentSubmitPurchaseOrderDecorator(_inner, null!, logger))).Should().Throw<ArgumentNullException>();
        ((Action)(() => _ = new IdempotentSubmitPurchaseOrderDecorator(_inner, _records, null!))).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task Rejects_a_null_command()
    {
        var act = () => _decorator.HandleAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
