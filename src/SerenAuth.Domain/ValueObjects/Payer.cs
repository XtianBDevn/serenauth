namespace SerenAuth.Domain.ValueObjects;

/// <summary>
/// An insurance payer. We don't pull from a payer registry in the MVP;
/// the value is normalized + length-validated so it stays comparable.
/// </summary>
public sealed record Payer
{
    private const int MaxLength = 120;

    public string Name { get; }

    private Payer(string name) => Name = name;

    public static Payer Create(string raw)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(raw);
        var trimmed = raw.Trim();
        if (trimmed.Length > MaxLength)
        {
            throw new ArgumentException($"Payer name exceeds {MaxLength} characters.", nameof(raw));
        }
        return new Payer(trimmed);
    }

    public override string ToString() => Name;
}
