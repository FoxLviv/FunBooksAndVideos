using System.Text.Json;
using FluentAssertions;
using FunBooksAndVideos.Application.Idempotency;
using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Domain.Idempotency;

namespace FunBooksAndVideos.Application.Tests.Idempotency;

public sealed class IdempotencyJsonAndExceptionTests
{
    [Fact]
    public void Payload_mismatch_exception_carries_the_key_code_and_message()
    {
        var key = new IdempotencyKey("order-42");

        var exception = new IdempotencyPayloadMismatchException(key);

        exception.Key.Should().Be(key);
        exception.Code.Should().Be(IdempotencyPayloadMismatchException.ErrorCode);
        exception.Message.Should().Contain("order-42").And.Contain("different request payload");
    }

    [Fact]
    public void Stored_response_options_are_read_only_camel_case_with_enum_names()
    {
        IdempotencyJson.Options.IsReadOnly.Should().BeTrue();
        IdempotencyJson.Options.PropertyNamingPolicy.Should().Be(JsonNamingPolicy.CamelCase);

        var json = JsonSerializer.Serialize(new Sample(MembershipType.VideoClub, 48.50m), IdempotencyJson.Options);

        json.Should().Be("{\"membershipType\":\"VideoClub\",\"total\":48.50}");
        JsonSerializer.Deserialize<Sample>(json, IdempotencyJson.Options).Should().Be(new Sample(MembershipType.VideoClub, 48.50m));
    }

    private sealed record Sample(MembershipType MembershipType, decimal Total);
}
