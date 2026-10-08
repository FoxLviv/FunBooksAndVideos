using FluentAssertions;
using FunBooksAndVideos.Domain.Common;
using FunBooksAndVideos.Domain.ValueObjects;

namespace FunBooksAndVideos.Domain.Tests.ValueObjects;

public sealed class MoneyTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(0.01)]
    [InlineData(48.50)]
    [InlineData(1_000_000)]
    public void Of_accepts_non_negative_amounts_with_up_to_two_decimals(decimal amount)
    {
        var money = Money.Of(amount);

        money.Amount.Should().Be(amount);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-100)]
    public void Of_rejects_negative_amounts(decimal amount)
    {
        var act = () => Money.Of(amount);

        act.Should().Throw<DomainValidationException>().Which.Code.Should().Be("money.negative");
    }

    [Theory]
    [InlineData(0.001)]
    [InlineData(19.995)]
    [InlineData(1.0000001)]
    public void Of_rejects_more_than_two_decimals(decimal amount)
    {
        var act = () => Money.Of(amount);

        var exception = act.Should().Throw<DomainValidationException>().Which;
        exception.Code.Should().Be("money.precision");
        exception.Message.Should().Contain(amount.ToString(System.Globalization.CultureInfo.InvariantCulture), "the rejected input must be reported as sent, not rounded");
    }

    [Theory]
    [InlineData("10")]
    [InlineData("10.0")]
    [InlineData("10.00")]
    public void Of_normalises_the_scale_to_two_decimals(string literal)
    {
        var money = Money.Of(decimal.Parse(literal, System.Globalization.CultureInfo.InvariantCulture));

        money.Amount.Scale.Should().Be(Money.Scale);
        money.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture).Should().Be("10.00");
    }

    [Theory]
    [InlineData("10.000", "10.00")]
    [InlineData("0.0000", "0.00")]
    [InlineData("48.500", "48.50")]
    [InlineData("1.10", "1.10")]
    public void Of_reduces_a_larger_scale_to_two_decimals(string literal, string expected)
    {
        var money = Money.Of(decimal.Parse(literal, System.Globalization.CultureInfo.InvariantCulture));

        money.Amount.Scale.Should().Be(Money.Scale);
        money.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture).Should().Be(expected);
    }

    [Fact]
    public void Default_value_exposes_an_amount_with_two_decimals()
    {
        default(Money).Amount.Scale.Should().Be(Money.Scale);
        default(Money).Amount.ToString(System.Globalization.CultureInfo.InvariantCulture).Should().Be("0.00");
    }

    [Fact]
    public void Sum_of_amounts_with_mixed_scales_has_two_decimals()
    {
        var total = Money.Sum([Money.Of(10m), Money.Of(0.5m), Money.Of(1.250m), default]);

        total.Amount.Scale.Should().Be(Money.Scale);
        total.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture).Should().Be("11.75");
    }

    [Fact]
    public void Amounts_with_different_scales_are_equal()
    {
        Money.Of(10m).Should().Be(Money.Of(10.00m));
        (Money.Of(10m) == Money.Of(10.00m)).Should().BeTrue();
        Money.Of(10m).GetHashCode().Should().Be(Money.Of(10.00m).GetHashCode());
    }

    [Fact]
    public void Sum_adds_all_amounts()
    {
        var total = Money.Sum([Money.Of(19.50m), Money.Of(14.00m), Money.Of(15.00m)]);

        total.Should().Be(Money.Of(48.50m));
    }

    [Fact]
    public void Sum_of_nothing_is_zero()
    {
        Money.Sum([]).Should().Be(Money.Zero);
    }

    [Fact]
    public void Sum_rejects_null()
    {
        var act = () => Money.Sum(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Addition_operator_and_named_method_agree()
    {
        var left = Money.Of(1.25m);
        var right = Money.Of(2.75m);

        (left + right).Should().Be(Money.Of(4.00m));
        Money.Add(left, right).Should().Be(left + right);
    }

    [Fact]
    public void Comparison_operators_follow_the_amount()
    {
        var smaller = Money.Of(1m);
        var larger = Money.Of(2m);

        (smaller < larger).Should().BeTrue();
        (smaller <= larger).Should().BeTrue();
        (larger > smaller).Should().BeTrue();
        (larger >= smaller).Should().BeTrue();
        (smaller >= larger).Should().BeFalse();
        (smaller > larger).Should().BeFalse();
        (larger < smaller).Should().BeFalse();
        (larger <= smaller).Should().BeFalse();
        smaller.CompareTo(larger).Should().BeNegative();
        larger.CompareTo(smaller).Should().BePositive();
        smaller.CompareTo(Money.Of(1.00m)).Should().Be(0);
    }

    [Fact]
    public void Comparison_operators_treat_equal_amounts_correctly()
    {
        var one = Money.Of(1m);
        var alsoOne = Money.Of(1.00m);

        (one < alsoOne).Should().BeFalse();
        (one > alsoOne).Should().BeFalse();
        (one <= alsoOne).Should().BeTrue();
        (one >= alsoOne).Should().BeTrue();
    }

    [Fact]
    public void ToString_uses_two_decimals_and_the_invariant_culture()
    {
        Money.Of(48.5m).ToString().Should().Be("48.50");
        Money.Zero.ToString().Should().Be("0.00");
    }

    [Fact]
    public void Default_value_is_zero()
    {
        default(Money).Should().Be(Money.Zero);
    }
}
