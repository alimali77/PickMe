using PickMe.Domain.Common;

namespace PickMe.Domain.Entities;

public sealed class EmailVerificationToken : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = default!;
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? UsedAtUtc { get; private set; }

    private EmailVerificationToken() { }

    public static EmailVerificationToken Create(Guid id, Guid userId, string tokenHash, DateTime expiresAtUtc)
    {
        return new EmailVerificationToken
        {
            Id = id,
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAtUtc = expiresAtUtc,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    public bool IsValid(DateTime nowUtc) => UsedAtUtc is null && nowUtc < ExpiresAtUtc;

    public void MarkUsed() => UsedAtUtc = DateTime.UtcNow;
}

public sealed class PasswordResetToken : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = default!;
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? UsedAtUtc { get; private set; }

    private PasswordResetToken() { }

    public static PasswordResetToken Create(Guid id, Guid userId, string tokenHash, DateTime expiresAtUtc)
    {
        return new PasswordResetToken
        {
            Id = id,
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAtUtc = expiresAtUtc,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    public bool IsValid(DateTime nowUtc) => UsedAtUtc is null && nowUtc < ExpiresAtUtc;

    public void MarkUsed() => UsedAtUtc = DateTime.UtcNow;
}

public sealed class RefreshToken : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = default!;
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }
    public Guid? ReplacedByTokenId { get; private set; }

    private RefreshToken() { }

    public static RefreshToken Create(Guid id, Guid userId, string tokenHash, DateTime expiresAtUtc)
    {
        return new RefreshToken
        {
            Id = id,
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAtUtc = expiresAtUtc,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    public bool IsActive(DateTime nowUtc) => RevokedAtUtc is null && nowUtc < ExpiresAtUtc;

    public void Revoke(Guid? replacedBy)
    {
        RevokedAtUtc = DateTime.UtcNow;
        ReplacedByTokenId = replacedBy;
    }
}
