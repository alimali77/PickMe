using FluentAssertions;
using PickMe.Infrastructure.Security;
using Xunit;

namespace PickMe.UnitTests.Security;

public class PasswordHasherTests
{
    [Fact]
    public void Hash_And_Verify_Round_Trips()
    {
        var h = new BcryptPasswordHasher(4);
        var hash = h.Hash("S3cret!Pass");
        h.Verify("S3cret!Pass", hash).Should().BeTrue();
        h.Verify("wrong", hash).Should().BeFalse();
    }

    [Fact]
    public void Hash_Twice_Produces_Different_Strings()
    {
        var h = new BcryptPasswordHasher(4);
        var a = h.Hash("same");
        var b = h.Hash("same");
        a.Should().NotBe(b);
    }

    [Fact]
    public void Verify_Handles_Invalid_Hash_Gracefully()
    {
        var h = new BcryptPasswordHasher(4);
        h.Verify("x", "not-a-valid-bcrypt-hash").Should().BeFalse();
    }
}

public class TokenHasherTests
{
    [Fact]
    public void Sha256_Produces_64Char_Hex_And_Is_Deterministic()
    {
        var h = new Sha256TokenHasher();
        var a = h.Hash("abc");
        var b = h.Hash("abc");
        a.Should().Be(b);
        a.Length.Should().Be(64);
    }

    [Fact]
    public void OpaqueTokenGenerator_Produces_UrlSafe_Base64()
    {
        var g = new OpaqueTokenGenerator();
        var t = g.Generate(32);
        t.Should().NotBeNullOrWhiteSpace();
        t.Should().NotContain("+").And.NotContain("/").And.NotContain("=");
    }
}
