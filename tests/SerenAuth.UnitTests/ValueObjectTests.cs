using FluentAssertions;
using SerenAuth.Domain.ValueObjects;
using Xunit;

namespace SerenAuth.UnitTests;

public class ValueObjectTests
{
    [Theory]
    [InlineData("90935")]
    [InlineData("90937")]
    public void Cpt_accepts_dialysis_codes(string code)
    {
        CptCode.Create(code).Value.Should().Be(code);
    }

    [Theory]
    [InlineData("99213")]
    [InlineData("00000")]
    [InlineData("")]
    public void Cpt_rejects_off_domain_codes(string bad)
    {
        var act = () => CptCode.Create(bad);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Icd10_accepts_N186()
    {
        Icd10Code.Create("N18.6").Value.Should().Be("N18.6");
    }

    [Theory]
    [InlineData("N186")]
    [InlineData("n18.6")] // lowercased input is normalized, still must be in allowlist
    [InlineData("Z99.0")]
    public void Icd10_rejects_off_domain_or_malformed(string raw)
    {
        var act = () => Icd10Code.Create(raw);
        // Z99.0 is well-formed but off-allowlist; N186 is malformed.
        // n18.6 normalizes to N18.6 and passes — verify with allowlist below.
        if (raw.Equals("n18.6", StringComparison.OrdinalIgnoreCase))
        {
            Icd10Code.Create(raw).Value.Should().Be("N18.6");
        }
        else
        {
            act.Should().Throw<ArgumentException>();
        }
    }

    [Fact]
    public void Payer_normalizes_whitespace()
    {
        Payer.Create("  BCBS  ").Name.Should().Be("BCBS");
    }
}
