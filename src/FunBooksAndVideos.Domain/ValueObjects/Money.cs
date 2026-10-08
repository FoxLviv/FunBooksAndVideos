using System.Globalization;
using FunBooksAndVideos.Domain.Common;

namespace FunBooksAndVideos.Domain.ValueObjects;

/// <summary>
/// Non-negative monetary amount with at most two decimal places.
/// The shop operates in a single currency, so the currency is implicit.
/// </summary>
public readonly record struct Money : IComparable<Money>
{
    /// <summary>Number of decimal places an amount may carry.</summary>
    public const int Scale = 2;

    private static readonly decimal ScaleNormaliser = 0.00m;

    private readonly decimal _amount;

    private Money(decimal amount)
    {
        _amount = Normalise(amount);
    }

    /// <summary>The amount, always carrying exactly <see cref="Scale"/> decimal places (also for <c>default(Money)</c>).</summary>
    public decimal Amount => _amount.Scale == Scale ? _amount : Normalise(_amount);

    public static Money Zero => new(0m);

    /// <summary>Creates an amount, rejecting negative values and values with more than <see cref="Scale"/> decimals.</summary>
    public static Money Of(decimal amount)
    {
        if (amount < 0m)
        {
            throw new DomainValidationException("money.negative", $"Amount must not be negative, but was {amount.ToString(CultureInfo.InvariantCulture)}.");
        }

        if (decimal.Round(amount, Scale) != amount)
        {
            throw new DomainValidationException("money.precision", $"Amount must have at most {Scale} decimal places, but was {amount.ToString(CultureInfo.InvariantCulture)}.");
        }

        return new Money(amount);
    }

    public static Money Sum(IEnumerable<Money> amounts)
    {
        ArgumentNullException.ThrowIfNull(amounts);

        var total = Zero;
        foreach (var amount in amounts)
        {
            total += amount;
        }

        return total;
    }

    public static Money operator +(Money left, Money right) => new(left.Amount + right.Amount);

    public static Money Add(Money left, Money right) => left + right;

    public static bool operator <(Money left, Money right) => left.Amount < right.Amount;

    public static bool operator >(Money left, Money right) => left.Amount > right.Amount;

    public static bool operator <=(Money left, Money right) => left.Amount <= right.Amount;

    public static bool operator >=(Money left, Money right) => left.Amount >= right.Amount;

    public int CompareTo(Money other) => Amount.CompareTo(other.Amount);

    public override string ToString() => Format(Amount);

    // Round drops surplus trailing zeros (10.000 -> 10.00) and adding 0.00m widens a smaller scale (10 -> 10.00);
    // the value itself never changes because Of() already rejected amounts with more than Scale decimals.
    private static decimal Normalise(decimal amount) => decimal.Round(amount, Scale) + ScaleNormaliser;

    private static string Format(decimal amount) => amount.ToString("0.00", CultureInfo.InvariantCulture);
}
