using FluentAssertions;
using FunBooksAndVideos.Application.Idempotency;
using FunBooksAndVideos.Application.PurchaseOrders.SubmitPurchaseOrder;
using FunBooksAndVideos.Domain.Customers;
using FunBooksAndVideos.Domain.Idempotency;

namespace FunBooksAndVideos.Application.Tests.Idempotency;

public sealed class RequestFingerprintTests
{
    private static SubmitPurchaseOrderCommand Command(long customerId = 4567890, decimal? expectedTotal = 48.50m, IdempotencyKey? key = null, params OrderLineRequest[] lines) =>
        new(customerId, lines.Length == 0 ? [new ProductLineRequest(1), new MembershipLineRequest(MembershipType.BookClub)] : lines, expectedTotal, key);

    [Fact]
    public void Is_a_lowercase_hex_sha256()
    {
        var fingerprint = RequestFingerprint.Compute(Command());

        fingerprint.Should().HaveLength(64).And.MatchRegex("^[0-9a-f]{64}$");
    }

    [Fact]
    public void The_same_payload_gives_the_same_fingerprint()
    {
        RequestFingerprint.Compute(Command()).Should().Be(RequestFingerprint.Compute(Command()));
    }

    [Fact]
    public void A_different_customer_gives_a_different_fingerprint()
    {
        RequestFingerprint.Compute(Command(customerId: 1)).Should().NotBe(RequestFingerprint.Compute(Command(customerId: 2)));
    }

    [Fact]
    public void A_different_expected_total_gives_a_different_fingerprint()
    {
        RequestFingerprint.Compute(Command(expectedTotal: 48.50m)).Should().NotBe(RequestFingerprint.Compute(Command(expectedTotal: null)));
        RequestFingerprint.Compute(Command(expectedTotal: 48.50m)).Should().NotBe(RequestFingerprint.Compute(Command(expectedTotal: 48.51m)));
    }

    [Theory]
    [InlineData("48.5")]
    [InlineData("48.50")]
    [InlineData("48.500")]
    [InlineData("048.5000")]
    public void Equal_totals_written_with_different_scales_give_the_same_fingerprint(string literal)
    {
        var total = decimal.Parse(literal, System.Globalization.CultureInfo.InvariantCulture);

        RequestFingerprint.Compute(Command(expectedTotal: total)).Should().Be(RequestFingerprint.Compute(Command(expectedTotal: 48.5m)));
    }

    [Fact]
    public void The_line_order_matters()
    {
        var first = Command(lines: [new ProductLineRequest(1), new ProductLineRequest(2)]);
        var second = Command(lines: [new ProductLineRequest(2), new ProductLineRequest(1)]);

        RequestFingerprint.Compute(first).Should().NotBe(RequestFingerprint.Compute(second));
    }

    [Fact]
    public void A_product_line_and_a_membership_line_with_the_same_number_never_match()
    {
        var product = Command(lines: [new ProductLineRequest((long)MembershipType.BookClub)]);
        var membership = Command(lines: [new MembershipLineRequest(MembershipType.BookClub)]);

        RequestFingerprint.Compute(product).Should().NotBe(RequestFingerprint.Compute(membership));
    }

    [Fact]
    public void The_idempotency_key_is_excluded_via_WithoutIdempotencyKey()
    {
        var withKey = Command(key: new IdempotencyKey("first"));
        var withOtherKey = Command(key: new IdempotencyKey("second"));
        var withoutKey = Command();

        withKey.WithoutIdempotencyKey().IdempotencyKey.Should().BeNull();
        RequestFingerprint.Compute(withKey.WithoutIdempotencyKey())
            .Should().Be(RequestFingerprint.Compute(withOtherKey.WithoutIdempotencyKey()))
            .And.Be(RequestFingerprint.Compute(withoutKey));
        RequestFingerprint.Compute(withKey).Should().NotBe(RequestFingerprint.Compute(withOtherKey));
    }

    [Fact]
    public void Rejects_null()
    {
        var act = () => RequestFingerprint.Compute<SubmitPurchaseOrderCommand>(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
