namespace SerenAuth.Domain.ValueObjects;

/// <summary>
/// Strongly-typed CPT procedure code. MVP scope is limited to dialysis-
/// relevant codes — broader catalogs are rejected so we never accept an
/// out-of-domain code silently.
/// </summary>
public sealed record CptCode
{
    /// <summary>Dialysis CPT codes recognized by SerenAuth.</summary>
    public static readonly IReadOnlySet<string> Allowed = new HashSet<string>(StringComparer.Ordinal)
    {
        "90935", // Hemodialysis, single physician evaluation
        "90937"  // Hemodialysis, repeated evaluation
    };

    public string Value { get; }

    private CptCode(string value) => Value = value;

    public static CptCode Create(string raw)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(raw);
        var trimmed = raw.Trim();
        if (!Allowed.Contains(trimmed))
        {
            throw new ArgumentOutOfRangeException(
                nameof(raw),
                $"CPT '{trimmed}' is not in the dialysis MVP allowlist.");
        }
        return new CptCode(trimmed);
    }

    public override string ToString() => Value;
}
