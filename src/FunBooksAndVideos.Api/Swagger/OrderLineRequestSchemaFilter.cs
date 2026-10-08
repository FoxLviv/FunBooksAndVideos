using System.Text.Json.Nodes;
using FunBooksAndVideos.Api.Contracts;
using FunBooksAndVideos.Application.Dtos;
using FunBooksAndVideos.Domain.Customers;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FunBooksAndVideos.Api.Swagger;

/// <summary>
/// Documents <see cref="OrderLineRequestModel"/> as the discriminated union it is on the wire: a <c>oneOf</c> of
/// <see cref="ProductLineSchemaId"/> and <see cref="MembershipLineSchemaId"/> selected by <c>type</c>. The runtime
/// model stays flat; only the OpenAPI description changes.
/// </summary>
internal sealed class OrderLineRequestSchemaFilter : ISchemaFilter
{
    public const string ProductLineSchemaId = "ProductLineRequest";

    public const string MembershipLineSchemaId = "MembershipLineRequest";

    private const string DiscriminatorProperty = "type";

    /// <inheritdoc />
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(context);

        if (context.Type != typeof(OrderLineRequestModel) || schema is not OpenApiSchema lineSchema)
        {
            return;
        }

        var productLine = AddDefinition(context.SchemaRepository, ProductLineSchemaId, ProductLineSchema);
        var membershipLine = AddDefinition(context.SchemaRepository, MembershipLineSchemaId, MembershipLineSchema);

        lineSchema.Type = null;
        lineSchema.Format = null;
        lineSchema.Properties = null;
        lineSchema.Required = null;
        lineSchema.AdditionalProperties = null;
        lineSchema.AdditionalPropertiesAllowed = true;
        lineSchema.Example = null;
        lineSchema.OneOf = [productLine, membershipLine];
        lineSchema.Discriminator = new OpenApiDiscriminator
        {
            PropertyName = DiscriminatorProperty,
            Mapping = new Dictionary<string, OpenApiSchemaReference>(StringComparer.Ordinal)
            {
                [nameof(OrderLineType.Product)] = new OpenApiSchemaReference(ProductLineSchemaId),
                [nameof(OrderLineType.Membership)] = new OpenApiSchemaReference(MembershipLineSchemaId),
            },
        };
    }

    private static OpenApiSchemaReference AddDefinition(SchemaRepository repository, string schemaId, Func<OpenApiSchema> create)
    {
        if (!repository.Schemas.ContainsKey(schemaId))
        {
            repository.AddDefinition(schemaId, create());
        }

        return new OpenApiSchemaReference(schemaId);
    }

    private static OpenApiSchema ProductLineSchema() => new()
    {
        Type = JsonSchemaType.Object,
        Description = "A catalog product (book or video) bought once.",
        Required = new HashSet<string>(StringComparer.Ordinal) { DiscriminatorProperty, "productId" },
        AdditionalPropertiesAllowed = false,
        Properties = new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal)
        {
            [DiscriminatorProperty] = TypeSchema(nameof(OrderLineType.Product)),
            ["productId"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Integer,
                Format = "int64",
                Minimum = "1",
                Description = "Catalog product id.",
                Example = JsonValue.Create(2),
            },
        },
    };

    private static OpenApiSchema MembershipLineSchema() => new()
    {
        Type = JsonSchemaType.Object,
        Description = "A club membership, activated on the customer account when the order is processed.",
        Required = new HashSet<string>(StringComparer.Ordinal) { DiscriminatorProperty, "membershipType" },
        AdditionalPropertiesAllowed = false,
        Properties = new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal)
        {
            [DiscriminatorProperty] = TypeSchema(nameof(OrderLineType.Membership)),
            ["membershipType"] = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Description = "Membership to buy.",
                Enum = Enum.GetNames<MembershipType>().Select(name => (JsonNode)JsonValue.Create(name)).ToList(),
                Example = JsonValue.Create(nameof(MembershipType.BookClub)),
            },
        },
    };

    private static OpenApiSchema TypeSchema(string value) => new()
    {
        Type = JsonSchemaType.String,
        Description = "Line discriminator.",
        Enum = [JsonValue.Create(value)],
        Example = JsonValue.Create(value),
    };
}
