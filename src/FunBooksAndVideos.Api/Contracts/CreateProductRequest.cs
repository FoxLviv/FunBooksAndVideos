using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using FunBooksAndVideos.Application.Catalog.CreateProduct;
using FunBooksAndVideos.Domain.Catalog;

namespace FunBooksAndVideos.Api.Contracts;

/// <summary>Product to add to the catalog.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CreateProductRequest
{
    /// <summary>Book (physical, shipped) or Video (digital).</summary>
    /// <example>Book</example>
    [Required]
    [EnumDataType(typeof(ProductKind))]
    public ProductKind? Kind { get; init; }

    /// <summary>Title.</summary>
    /// <example>The Pragmatic Programmer</example>
    [Required]
    [StringLength(Product.MaxNameLength)]
    public string Name { get; init; } = string.Empty;

    /// <summary>Unit price with at most two decimals.</summary>
    /// <example>42.00</example>
    [Required]
    [Range(typeof(decimal), "0", "1000000", ParseLimitsInInvariantCulture = true, ErrorMessage = "The price must be between 0 and 1,000,000.")]
    public decimal? Price { get; init; }

    public CreateProductCommand ToCommand() =>
        new(
            Kind ?? throw new InvalidOperationException("The request was not validated before conversion."),
            Name,
            Price ?? throw new InvalidOperationException("The request was not validated before conversion."));
}
