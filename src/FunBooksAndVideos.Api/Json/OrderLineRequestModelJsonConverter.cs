using System.Text.Json;
using System.Text.Json.Serialization;
using FunBooksAndVideos.Api.Contracts;
using FunBooksAndVideos.Application.Dtos;
using FunBooksAndVideos.Domain.Customers;

namespace FunBooksAndVideos.Api.Json;

/// <summary>
/// Reads an <see cref="OrderLineRequestModel"/> while enforcing the discriminated shape the OpenAPI document
/// promises: unknown members are rejected, and the property of the other branch is rejected even when it is
/// explicitly <c>null</c> (a product line never carries <c>membershipType</c>, a membership line never carries
/// <c>productId</c>). Enum values are parsed with the configured converters, so names stay case-insensitive.
/// </summary>
internal sealed class OrderLineRequestModelJsonConverter : JsonConverter<OrderLineRequestModel>
{
    private const string TypeProperty = "type";
    private const string ProductIdProperty = "productId";
    private const string MembershipTypeProperty = "membershipType";

    public override OrderLineRequestModel Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("An order line must be a JSON object.");
        }

        OrderLineType? type = null;
        long? productId = null;
        MembershipType? membershipType = null;
        var hasProductId = false;
        var hasMembershipType = false;

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            var name = reader.GetString() ?? string.Empty;
            reader.Read();

            if (string.Equals(name, TypeProperty, StringComparison.OrdinalIgnoreCase))
            {
                type = ReadEnum<OrderLineType>(ref reader, options, TypeProperty);
            }
            else if (string.Equals(name, ProductIdProperty, StringComparison.OrdinalIgnoreCase))
            {
                hasProductId = true;
                productId = ReadProductId(ref reader);
            }
            else if (string.Equals(name, MembershipTypeProperty, StringComparison.OrdinalIgnoreCase))
            {
                hasMembershipType = true;
                membershipType = ReadEnum<MembershipType>(ref reader, options, MembershipTypeProperty);
            }
            else
            {
                throw new JsonException($"The JSON property '{name}' is not a member of an order line; allowed members are {TypeProperty}, {ProductIdProperty} and {MembershipTypeProperty}.");
            }
        }

        if (type == OrderLineType.Product && hasMembershipType)
        {
            throw new JsonException($"A product line must not carry {MembershipTypeProperty}, not even as null.");
        }

        if (type == OrderLineType.Membership && hasProductId)
        {
            throw new JsonException($"A membership line must not carry {ProductIdProperty}, not even as null.");
        }

        return new OrderLineRequestModel { Type = type, ProductId = productId, MembershipType = membershipType };
    }

    public override void Write(Utf8JsonWriter writer, OrderLineRequestModel value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);

        writer.WriteStartObject();
        writer.WritePropertyName(TypeProperty);
        JsonSerializer.Serialize(writer, value.Type, options);

        if (value.ProductId is { } productId)
        {
            writer.WriteNumber(ProductIdProperty, productId);
        }

        if (value.MembershipType is { } membershipType)
        {
            writer.WritePropertyName(MembershipTypeProperty);
            JsonSerializer.Serialize(writer, membershipType, options);
        }

        writer.WriteEndObject();
    }

    // A converter cannot learn the element index of the line it is reading, so the JSON path reported by MVC
    // ends at the line ("$.lines[0]"); the property name is therefore carried in the message instead.
    private static TEnum? ReadEnum<TEnum>(ref Utf8JsonReader reader, JsonSerializerOptions options, string property)
        where TEnum : struct, Enum
    {
        try
        {
            return JsonSerializer.Deserialize<TEnum?>(ref reader, options);
        }
        catch (JsonException exception)
        {
            throw new JsonException($"{property}: {exception.Message}", exception);
        }
    }

    private static long? ReadProductId(ref Utf8JsonReader reader)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt64(out var productId))
        {
            return productId;
        }

        throw new JsonException($"The {ProductIdProperty} must be an integer.");
    }
}
