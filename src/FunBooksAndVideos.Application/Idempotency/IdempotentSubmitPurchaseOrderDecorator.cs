using System.Text.Json;
using FunBooksAndVideos.Application.Abstractions;
using FunBooksAndVideos.Application.PurchaseOrders.SubmitPurchaseOrder;
using FunBooksAndVideos.Domain.Idempotency;
using FunBooksAndVideos.Domain.Persistence;
using Microsoft.Extensions.Logging;

namespace FunBooksAndVideos.Application.Idempotency;

/// <summary>
/// GoF Decorator that makes "submit a purchase order" idempotent per client-supplied <see cref="IdempotencyKey"/>.
/// A key seen before with the same payload replays the stored result (no second order); with a different payload
/// it is rejected. The record itself is written by the handler in the same unit of work as the order, so a key
/// is either stored together with its order or not at all. Commands without a key pass straight through.
/// </summary>
public sealed partial class IdempotentSubmitPurchaseOrderDecorator : ICommandHandler<SubmitPurchaseOrderCommand, SubmitPurchaseOrderResult>
{
    private readonly ICommandHandler<SubmitPurchaseOrderCommand, SubmitPurchaseOrderResult> _inner;
    private readonly IIdempotencyRecordRepository _records;
    private readonly ILogger<IdempotentSubmitPurchaseOrderDecorator> _logger;

    public IdempotentSubmitPurchaseOrderDecorator(
        ICommandHandler<SubmitPurchaseOrderCommand, SubmitPurchaseOrderResult> inner,
        IIdempotencyRecordRepository records,
        ILogger<IdempotentSubmitPurchaseOrderDecorator> logger)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(records);
        ArgumentNullException.ThrowIfNull(logger);

        _inner = inner;
        _records = records;
        _logger = logger;
    }

    public async Task<SubmitPurchaseOrderResult> HandleAsync(SubmitPurchaseOrderCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.IdempotencyKey is not { } key)
        {
            return await _inner.HandleAsync(command, cancellationToken);
        }

        var fingerprint = RequestFingerprint.Compute(command.WithoutIdempotencyKey());

        var existing = await _records.FindAsync(key, cancellationToken);
        if (existing is not null)
        {
            return Replay(existing, fingerprint);
        }

        try
        {
            return await _inner.HandleAsync(command, cancellationToken);
        }
        catch (DuplicateEntityException ex) when (ex.EntityName == IdempotencyRecord.EntityName)
        {
            // A concurrent request with the same key committed first; its result is the one to replay.
            var winner = await _records.FindAsync(key, cancellationToken);
            if (winner is null)
            {
                throw;
            }

            return Replay(winner, fingerprint);
        }
    }

    private SubmitPurchaseOrderResult Replay(IdempotencyRecord record, string fingerprint)
    {
        if (!string.Equals(record.RequestFingerprint, fingerprint, StringComparison.Ordinal))
        {
            throw new IdempotencyPayloadMismatchException(record.Key);
        }

        var stored = JsonSerializer.Deserialize<SubmitPurchaseOrderResult>(record.ResponsePayload, IdempotencyJson.Options)
            ?? throw new InvalidOperationException($"The stored response of idempotency key '{record.Key}' is empty.");

        LogReplayed(_logger, record.Key.Value, stored.PurchaseOrder.Id);

        return stored with { IdempotentReplay = true };
    }

    [LoggerMessage(EventId = 2100, Level = LogLevel.Information, Message = "Idempotency key {IdempotencyKey} replayed: returning the stored result of purchase order {PurchaseOrderId}.")]
    private static partial void LogReplayed(ILogger logger, string idempotencyKey, long purchaseOrderId);
}
