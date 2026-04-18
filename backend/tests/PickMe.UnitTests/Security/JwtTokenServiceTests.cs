using System.IdentityModel.Tokens.Jwt;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PickMe.Infrastructure.Security;
using Xunit;

namespace PickMe.UnitTests.Security;

public class JwtTokenServiceTests
{
    private static JwtTokenService NewService(string? secret = null) =>
        new(Options.Create(new JwtOptions
        {
            Secret = secret ?? "test-secret-that-is-definitely-32-chars-or-more-aaaa",
            Issuer = "test-iss",
            Audience = "test-aud",
            AccessTtlMinutes = 60,
            RefreshTtlDays = 7,
        }));

    [Fact]
    public void Access_Token_Is_Signed_And_Contains_Expected_Claims()
    {
        var svc = NewService();
        var userId = Guid.NewGuid();
        var result = svc.CreateAccessToken(userId, "a@b.tr", "Customer");

        result.Token.Should().NotBeNullOrWhiteSpace();
        result.ExpiresAtUtc.Should().BeAfter(DateTime.UtcNow);

        var handler = new JwtSecurityTokenHandler();
        var parameters = new TokenValidationParameters
        {
            ValidIssuer = "test-iss",
            ValidAudience = "test-aud",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("test-secret-that-is-definitely-32-chars-or-more-aaaa")),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
        };

        var principal = handler.ValidateToken(result.Token, parameters, out _);
        principal.Identity.Should().NotBeNull();
        principal.FindFirst("role")?.Value.Should().Be("Customer");
    }

    [Fact]
    public void Short_Secret_Is_Rejected()
    {
        var svc = NewService("short");
        FluentActions.Invoking(() => svc.CreateAccessToken(Guid.NewGuid(), "x@y.tr", "Admin"))
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*256*");
    }

    [Fact]
    public void Refresh_Token_Values_Are_Unique_And_Url_Safe()
    {
        var svc = NewService();
        var a = svc.CreateRefreshTokenValue();
        var b = svc.CreateRefreshTokenValue();
        a.Should().NotBe(b);
        a.Should().NotContain("+").And.NotContain("/").And.NotContain("=");
    }
}
