using FluentAssertions;
using FunBooksAndVideos.Api.ErrorHandling;

namespace FunBooksAndVideos.Api.Tests.ErrorHandling;

public sealed class ModelStateKeysTests
{
    [Theory]
    [InlineData("CustomerId", "customerId")]
    [InlineData("Lines", "lines")]
    [InlineData("Lines[0]", "lines[0]")]
    [InlineData("Lines[0].ProductId", "lines[0].productId")]
    [InlineData("Lines[12].MembershipType", "lines[12].membershipType")]
    [InlineData("ShippingAddress.City", "shippingAddress.city")]
    [InlineData("customerId", "customerId")]
    public void Member_paths_are_converted_to_camel_case_json_names(string key, string expected)
    {
        ModelStateKeys.ToJsonKey(key).Should().Be(expected);
    }

    [Theory]
    [InlineData("$.lines[0].type")]
    [InlineData("$")]
    [InlineData("Idempotency-Key")]
    [InlineData("")]
    public void Json_paths_header_names_and_empty_keys_are_left_unchanged(string key)
    {
        ModelStateKeys.ToJsonKey(key).Should().Be(key);
    }

    [Fact]
    public void Keys_that_collapse_onto_the_same_json_key_keep_all_their_messages()
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["Lines[0].ProductId"] = ["first"],
            ["lines[0].productId"] = ["second"],
            ["$.lines[0].type"] = ["third"],
        };

        var result = ModelStateKeys.ToJsonKeys(errors);

        result.Should().HaveCount(2);
        result["lines[0].productId"].Should().Equal("first", "second");
        result["$.lines[0].type"].Should().Equal("third");
    }
}
