using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using FunBooksAndVideos.Api.Contracts;
using FunBooksAndVideos.Api.Json;
using FunBooksAndVideos.Application.Dtos;
using FunBooksAndVideos.Domain.Customers;

namespace FunBooksAndVideos.Api.Tests.Json;

public sealed class OrderLineRequestModelJsonConverterTests
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new StrictStringEnumConverterFactory());
        options.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
        return options;
    }

    [Fact]
    public void Round_trips_a_product_line()
    {
        var line = new OrderLineRequestModel { Type = OrderLineType.Product, ProductId = 42 };

        var json = JsonSerializer.Serialize(line, Options);
        var back = JsonSerializer.Deserialize<OrderLineRequestModel>(json, Options);

        json.Should().Be("{\"type\":\"Product\",\"productId\":42}");
        back.Should().BeEquivalentTo(line);
    }

    [Fact]
    public void Round_trips_a_membership_line()
    {
        var line = new OrderLineRequestModel { Type = OrderLineType.Membership, MembershipType = MembershipType.Premium };

        var json = JsonSerializer.Serialize(line, Options);
        var back = JsonSerializer.Deserialize<OrderLineRequestModel>(json, Options);

        json.Should().Be("{\"type\":\"Membership\",\"membershipType\":\"Premium\"}");
        back.Should().BeEquivalentTo(line);
    }

    [Fact]
    public void Writes_a_missing_type_as_null_and_omits_absent_properties()
    {
        JsonSerializer.Serialize(new OrderLineRequestModel(), Options).Should().Be("{\"type\":null}");
    }

    [Fact]
    public void Reads_property_names_case_insensitively_and_keeps_the_last_duplicate()
    {
        var line = JsonSerializer.Deserialize<OrderLineRequestModel>("{\"TYPE\":\"product\",\"ProductId\":1,\"productId\":7}", Options);

        line.Should().BeEquivalentTo(new OrderLineRequestModel { Type = OrderLineType.Product, ProductId = 7 });
    }

    [Theory]
    [InlineData("\"not an object\"", "JSON object")]
    [InlineData("{\"type\":\"Product\",\"productId\":\"1\"}", "integer")]
    [InlineData("{\"type\":\"Product\",\"productId\":1.5}", "integer")]
    [InlineData("{\"type\":\"Product\",\"productId\":1,\"membershipType\":null}", "membershipType")]
    [InlineData("{\"type\":\"Membership\",\"membershipType\":\"BookClub\",\"productId\":null}", "productId")]
    [InlineData("{\"type\":\"Product\",\"productId\":1,\"quantity\":2}", "'quantity'")]
    [InlineData("{\"type\":\"Membership\",\"membershipType\":\"BookClub, VideoClub\"}", "membershipType")]
    [InlineData("{\"type\":3,\"productId\":1}", "type")]
    public void Rejects_invalid_shapes_with_a_message_naming_the_problem(string json, string expectedInMessage)
    {
        var act = () => JsonSerializer.Deserialize<OrderLineRequestModel>(json, Options);

        act.Should().Throw<JsonException>().WithMessage($"*{expectedInMessage}*");
    }

    [Fact]
    public void Write_rejects_null_arguments()
    {
        var converter = new OrderLineRequestModelJsonConverter();
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        var nullWriter = () => converter.Write(null!, new OrderLineRequestModel(), Options);
        var nullValue = () => converter.Write(writer, null!, Options);

        nullWriter.Should().Throw<ArgumentNullException>();
        nullValue.Should().Throw<ArgumentNullException>();
    }
}
