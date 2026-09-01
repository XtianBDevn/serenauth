using FluentAssertions;
using SerenAuth.Infrastructure.Security;
using Xunit;

namespace SerenAuth.UnitTests;

public class PasswordHasherTests
{
    private readonly PasswordHasher _sut = new();

    [Fact]
    public void Hash_then_verify_round_trips()
    {
        var (hash, salt) = _sut.Hash("Sup3rSecret!");
        _sut.Verify("Sup3rSecret!", hash, salt).Should().BeTrue();
    }

    [Fact]
    public void Verify_rejects_wrong_password()
    {
        var (hash, salt) = _sut.Hash("Sup3rSecret!");
        _sut.Verify("wrong", hash, salt).Should().BeFalse();
    }

    [Fact]
    public void Hash_produces_unique_salt_per_call()
    {
        var (h1, s1) = _sut.Hash("same-password");
        var (h2, s2) = _sut.Hash("same-password");
        s1.Should().NotBe(s2);
        h1.Should().NotBe(h2);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Hashing_an_empty_password_throws(string bad)
    {
        var act = () => _sut.Hash(bad);
        act.Should().Throw<ArgumentException>();
    }
}
