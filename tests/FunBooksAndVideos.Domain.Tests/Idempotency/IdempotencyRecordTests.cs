using FluentAssertions;
using FunBooksAndVideos.Domain.Common;
using FunBooksAndVideos.Domain.Idempotency;

namespace FunBooksAndVideos.Domain.Tests.Idempotency;

public sealed class IdempotencyRecordTests
{
    private static readonly IdempotencyKey Key = new("order-42");
    private static readonly DateTimeOffset CreatedAt = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Keeps_what_it_was_given()
    {
        var record = new IdempotencyRecord(Key, " abc123 ", "{\"id\":1}", CreatedAt);

        record.Key.Should().Be(Key);
        record.RequestFingerprint.Should().Be("abc123");
        record.ResponsePayload.Should().Be("{\"id\":1}");
        record.CreatedAt.Should().Be(CreatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Rejects_a_blank_fingerprint(string? fingerprint)
    {
        var act = () => new IdempotencyRecord(Key, fingerprint!, "{}", CreatedAt);

        act.Should().Throw<DomainValidationException>().Which.Code.Should().Be("idempotency_record.fingerprint.invalid");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Rejects_a_blank_response_payload(string? payload)
    {
        var act = () => new IdempotencyRecord(Key, "abc", payload!, CreatedAt);

        act.Should().Throw<DomainValidationException>().Which.Code.Should().Be("idempotency_record.response.invalid");
    }

    [Fact]
    public void Rejects_a_default_key()
    {
        var act = () => new IdempotencyRecord(default, "abc", "{}", CreatedAt);

        act.Should().Throw<DomainValidationException>().Which.Code.Should().Be("idempotency_record.key.invalid");
    }
}
