using System.Text.RegularExpressions;

namespace SerenAuth.Domain.ValueObjects;

/// <summary>
/// ICD-10 diagnosis code. We validate against the standard format
/// (Letter, two digits, optional dot + 1-4 alphanumerics) and the
/// MVP-scoped dialysis allowlist.
/// </summary>
public sealed partial record Icd10Code
{
    public static readonly IReadOnlySet<string> Allowed = new HashSet<string>(StringComparer.Ordinal)
    {
        "N18.6" // End-stage renal disease
    };

    [GeneratedRegex(@"^[A-Z][0-9]{2}(?:\.[A-Z0-9]{1,4})?$", RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();

    public string Value { get; }

    private Icd10Code(string value) => Value = value;

    public static Icd10Code Create(string raw)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(raw);
        var trimmed = raw.Trim().ToUpperInvariant();
        if (!Pattern().IsMatch(trimmed))
        {
            throw new ArgumentException($"ICD-10 code '{raw}' is malformed.", nameof(raw));
        }
        if (!Allowed.Contains(trimmed))
        {
            throw new ArgumentOutOfRangeException(
                nameof(raw),
                $"ICD-10 '{trimmed}' is not in the dialysis MVP allowlist.");
        }
        return new Icd10Code(trimmed);
    }

    public override string ToString() => Value;
}
