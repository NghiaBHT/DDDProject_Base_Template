using DDDProject.Domain.Common;

namespace DDDProject.Domain.ValueObjects;

/// <summary>
/// Represents a monetary value.
/// </summary>
public class Money : ValueObject
{
    public string Currency { get; private set; }
    public decimal Amount { get; private set; }

    // Required for EF Core - can be private if not used elsewhere
    private Money() { }

    public Money(string currency, decimal amount)
    {
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency code cannot be empty.", nameof(currency));
        if (currency.Length != 3) // Basic validation, could be more robust
            throw new ArgumentException("Currency code must be 3 letters.", nameof(currency));
        if (amount < 0)
            throw new ArgumentException("Money amount cannot be negative.", nameof(amount));

        Currency = currency.ToUpperInvariant();
        Amount = amount;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Currency;
        yield return Amount;
    }

    public override string ToString()
    {
        return $"{Amount:0.00} {Currency}";
    }

    // Optional: Operator overloads for arithmetic (ensure currency matching)
    public static Money operator +(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException("Cannot add money of different currencies.");
        return new Money(a.Currency, a.Amount + b.Amount);
    }
    // Add other operators like -, *, / as needed
} 