namespace PickMe.Application.Abstractions;

public interface IPasswordHasher
{
    string Hash(string plain);
    bool Verify(string plain, string hash);
}

public interface ITokenHasher
{
    /// <summary>Deterministic hash for storing opaque tokens in DB (SHA-256).</summary>
    string Hash(string plainToken);
}

public interface IOpaqueTokenGenerator
{
    string Generate(int byteLength = 32);
}

public interface IJwtTokenService
{
    JwtAccessTokenResult CreateAccessToken(Guid userId, string email, string role);
    string CreateRefreshTokenValue();
    DateTime GetAccessTokenExpiresAtUtc(DateTime nowUtc);
    DateTime GetRefreshTokenExpiresAtUtc(DateTime nowUtc);
}

public sealed record JwtAccessTokenResult(string Token, DateTime ExpiresAtUtc);

public interface IClock
{
    DateTime UtcNow { get; }
}

public interface ICurrentUser
{
    Guid? UserId { get; }
    string? Email { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
}
