using System.Text.Json;
using FunBooksAndVideos.Application.Abstractions;
using FunBooksAndVideos.Application.Exceptions;
using FunBooksAndVideos.Application.Idempotency;
using FunBooksAndVideos.Application.Mapping;
using FunBooksAndVideos.Application.Processing;
using FunBooksAndVideos.Domain.Common;
using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Domain.Idempotency;
using FunBooksAndVideos.Domain.Orders;
using FunBooksAndVideos.Domain.Persistence;
using FunBooksAndVideos.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace FunBooksAndVideos.Application.PurchaseOrders.SubmitPurchaseOrder;

/// <summary>
/// Transaction script for "submit a purchase order": validate, build the order, run the processor,
/// persist everything in one unit of work. Any failure before the commit leaves no trace.
/// </summary>
public sealed partial class SubmitPurchaseOrderHandler : ICommandHandler<SubmitPurchaseOrderCommand, SubmitPurchaseOrderResult>
{
    private readonly ICustomerRepository _customers;
    private readonly IPurchaseOrderRepository _purchaseOrders;
    private readonly IOrderLineFactory _lineFactory;
    private readonly IPurchaseOrderProcessor _processor;
    private readonly IIdempotencyRecordRepository _idempotencyRecords;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<SubmitPurchaseOrderHandler> _logger;

    public SubmitPurchaseOrderHandler(
        ICustomerRepository customers,
        IPurchaseOrderRepository purchaseOrders,
        IOrderLineFactory lineFactory,
        IPurchaseOrderProcessor processor,
        IIdempotencyRecordRepository idempotencyRecords,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        ILogger<SubmitPurchaseOrderHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(customers);
        ArgumentNullException.ThrowIfNull(purchaseOrders);
        ArgumentNullException.ThrowIfNull(lineFactory);
        ArgumentNullException.ThrowIfNull(processor);
        ArgumentNullException.ThrowIfNull(idempotencyRecords);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _customers = customers;
        _purchaseOrders = purchaseOrders;
        _lineFactory = lineFactory;
        _processor = processor;
        _idempotencyRecords = idempotencyRecords;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<SubmitPurchaseOrderResult> HandleAsync(SubmitPurchaseOrderCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var customer = await FindCustomerAsync(command.CustomerId, cancellationToken);
        var expectedTotal = ParseExpectedTotal(command.ExpectedTotal);
        var lines = await _lineFactory.CreateAsync(command.Lines, cancellationToken);

        var orderId = await _purchaseOrders.NextIdentityAsync(cancellationToken);
        var now = _timeProvider.GetUtcNow();
        var order = PurchaseOrder.Create(orderId, customer.Id, lines, now);

        if (expectedTotal is { } expected && expected != order.Total)
        {
            throw ValidationException.For(
                "expectedTotal",
                $"Expected total {expected} does not match the calculated total {order.Total}.");
        }

        var processing = await _processor.ProcessAsync(order, customer, cancellationToken);

        // The result is built before the commit so that it can be stored with the order (idempotent replay).
        var result = new SubmitPurchaseOrderResult(
            PurchaseOrderMapper.ToDto(order),
            processing.EffectsOf<MembershipActivationEffect>().Select(effect => MembershipActivationMapper.ToDto(effect.Result)).ToList(),
            processing.EffectsOf<ShippingSlipGeneratedEffect>().Select(effect => ShippingSlipMapper.ToDto(effect.ShippingSlip)).SingleOrDefault(),
            processing.AppliedRules);

        _purchaseOrders.Add(order);

        if (command.IdempotencyKey is { } key)
        {
            _idempotencyRecords.Add(new IdempotencyRecord(
                key,
                RequestFingerprint.Compute(command.WithoutIdempotencyKey()),
                JsonSerializer.Serialize(result, IdempotencyJson.Options),
                now));
        }

        await _unitOfWork.CommitAsync(cancellationToken);

        LogSubmitted(_logger, order.Id, customer.Id, order.Total, order.Lines.Count);

        return result;
    }

    private static Money? ParseExpectedTotal(decimal? expectedTotal)
    {
        if (expectedTotal is not { } value)
        {
            return null;
        }

        try
        {
            return Money.Of(value);
        }
        catch (DomainValidationException ex)
        {
            throw ValidationException.For("expectedTotal", ex.Message);
        }
    }

    private async Task<Customer> FindCustomerAsync(long customerId, CancellationToken cancellationToken)
    {
        var customer = await _customers.FindAsync(new CustomerId(customerId), cancellationToken);
        return customer ?? throw ValidationException.For("customerId", $"Customer {customerId} does not exist.");
    }

    [LoggerMessage(EventId = 2000, Level = LogLevel.Information, Message = "Purchase order {PurchaseOrderId} submitted for customer {CustomerId}: total {Total}, {LineCount} line(s).")]
    private static partial void LogSubmitted(ILogger logger, PurchaseOrderId purchaseOrderId, CustomerId customerId, Money total, int lineCount);
}
