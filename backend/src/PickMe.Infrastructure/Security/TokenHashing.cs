using System.Security.Cryptography;
using System.Text;
using PickMe.Application.Abstractions;

namespace PickMe.Infrastructure.Security;

public sealed class Sha256TokenHasher : ITokenHasher
{
    public string Hash(string plainToken)
    {
        var bytes = Encoding.UTF8.GetBytes(plainToken);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}

public sealed class OpaqueTokenGenerator : IOpaqueTokenGenerator
{
    public string Generate(int byteLength = 32)
    {
        var buffer = new byte[byteLength];
        RandomNumberGenerator.Fill(buffer);
        return Convert.ToBase64String(buffer)
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
