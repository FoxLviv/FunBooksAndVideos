using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json.Serialization;
using FunBooksAndVideos.Application.PurchaseOrders.SubmitPurchaseOrder;
using FunBooksAndVideos.Domain.Idempotency;

namespace FunBooksAndVideos.Api.Contracts;

/// <summary>Purchase order to submit. Prices are never sent by the client; they come from the catalog.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class SubmitPurchaseOrderRequest : IValidatableObject
{
    /// <summary>Customer placing the order.</summary>
    /// <example>4567890</example>
    [Required]
    [Range(typeof(long), "1", "9223372036854775807", ParseLimitsInInvariantCulture = true, ErrorMessage = "The customer id must be a positive number.")]
    public long? CustomerId { get; init; }

    /// <summary>Upper bound on the number of item lines in one order; larger baskets are rejected with 400.</summary>
    public const int MaxLines = 100;

    /// <summary>Item lines; at least one, at most <see cref="MaxLines"/>.</summary>
    [Required]
    [MaxLength(MaxLines, ErrorMessage = "A purchase order may contain at most 100 item lines.")]
    [MinLength(1, ErrorMessage = "At least one item line is required.")]
    public IReadOnlyList<OrderLineRequestModel> Lines { get; init; } = [];

    /// <summary>
    /// Optional total the client displayed to the customer. When present it must equal the total
    /// calculated from catalog prices, otherwise the order is rejected with 422.
    /// </summary>
    /// <example>48.50</example>
    [Range(typeof(decimal), "0", "79228162514264337593543950335", ParseLimitsInInvariantCulture = true, ErrorMessage = "The expected total must not be negative.")]
    public decimal? ExpectedTotal { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // [Required] and [MinLength] do not look inside the list, so null entries ("lines": [null]) are reported here.
        if (Lines is null)
        {
            yield break;
        }

        for (var index = 0; index < Lines.Count; index++)
        {
            if (Lines[index] is null)
            {
                yield return new ValidationResult(
                    "Item line must not be null.",
                    [string.Create(CultureInfo.InvariantCulture, $"{nameof(Lines)}[{index}]")]);
            }
        }
    }

    /// <summary>Converts the validated request into the use-case command.</summary>
    /// <param name="idempotencyKey">Optional idempotency key taken from the <c>Idempotency-Key</c> header.</param>
    public SubmitPurchaseOrderCommand ToCommand(IdempotencyKey? idempotencyKey = null) =>
        new(
            CustomerId ?? throw new InvalidOperationException("The request was not validated before conversion: the customer id is missing."),
            Lines.Select(line => line?.ToRequest() ?? throw new InvalidOperationException("The order lines were not validated before conversion: a line is null.")).ToList(),
            ExpectedTotal,
            idempotencyKey);
}
