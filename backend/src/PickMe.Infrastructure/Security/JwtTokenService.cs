using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PickMe.Application.Abstractions;

namespace PickMe.Infrastructure.Security;

public sealed class JwtTokenService(IOptions<JwtOptions> options) : IJwtTokenService
{
    private readonly JwtOptions _opt = options.Value;

    public JwtAccessTokenResult CreateAccessToken(Guid userId, string email, string role)
    {
        var expires = DateTime.UtcNow.AddMinutes(_opt.AccessTtlMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Role, role),
            new("role", role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var keyBytes = Encoding.UTF8.GetBytes(_opt.Secret);
        if (keyBytes.Length < 32)
        {
            throw new InvalidOperationException("JWT_SECRET must be at least 256 bits (32 bytes).");
        }

        var creds = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _opt.Issuer,
            audience: _opt.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires,
            signingCredentials: creds);

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        return new JwtAccessTokenResult(jwt, expires);
    }

    public string CreateRefreshTokenValue()
    {
        var bytes = new byte[48];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    public DateTime GetAccessTokenExpiresAtUtc(DateTime nowUtc) => nowUtc.AddMinutes(_opt.AccessTtlMinutes);

    public DateTime GetRefreshTokenExpiresAtUtc(DateTime nowUtc) => nowUtc.AddDays(_opt.RefreshTtlDays);
}
