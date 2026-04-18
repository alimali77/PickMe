using PickMe.Application.Abstractions;

namespace PickMe.Infrastructure.Security;

public sealed class BcryptPasswordHasher(int workFactor = 12) : IPasswordHasher
{
    private readonly int _workFactor = workFactor;

    public string Hash(string plain) => BCrypt.Net.BCrypt.EnhancedHashPassword(plain, _workFactor);

    public bool Verify(string plain, string hash)
    {
        try { return BCrypt.Net.BCrypt.EnhancedVerify(plain, hash); }
        catch (BCrypt.Net.SaltParseException) { return false; }
    }
}
