using DDDProject.Domain.Common;

namespace DDDProject.Domain.ValueObjects;

/// <summary>
/// Represents a Stock Keeping Unit (SKU).
/// </summary>
public class Sku : ValueObject
{
    public string Value { get; private set; }

    // Max length for SKU - adjust as needed
    private const int MaxLength = 50;

    // Required for EF Core
    private Sku() { }

    private Sku(string value)
    {
        Value = value;
    }

    public static Sku? Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            // Consider if empty SKU is valid; returning null might be one option
            // Or throw validation exception if it must exist
            return null; // Or throw new ArgumentException("SKU cannot be empty.");
        }

        if (value.Length > MaxLength)
        {
            throw new ArgumentOutOfRangeException(nameof(value), $"SKU cannot be longer than {MaxLength} characters.");
        }

        // Add more validation rules if needed (e.g., allowed characters)

        return new Sku(value);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString()
    {
        return Value;
    }
} 