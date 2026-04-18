using PickMe.Domain.Common;

namespace PickMe.Domain.Entities;

public sealed class User : Entity<Guid>
{
    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public UserRole Role { get; private set; }
    public bool EmailConfirmed { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime? LastLoginAtUtc { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public DateTime? LockedUntilUtc { get; private set; }

    private User() { }

    public static User Create(Guid id, string email, string passwordHash, UserRole role)
    {
        return new User
        {
            Id = id,
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            Role = role,
            EmailConfirmed = false,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    public void ConfirmEmail()
    {
        EmailConfirmed = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdatePasswordHash(string newHash)
    {
        PasswordHash = newHash;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RecordSuccessfulLogin()
    {
        LastLoginAtUtc = DateTime.UtcNow;
        FailedLoginAttempts = 0;
        LockedUntilUtc = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RecordFailedLogin(int maxAttempts, int lockoutMinutes)
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= maxAttempts)
        {
            LockedUntilUtc = DateTime.UtcNow.AddMinutes(lockoutMinutes);
        }
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public bool IsLocked(DateTime nowUtc) => LockedUntilUtc.HasValue && LockedUntilUtc > nowUtc;

    public void SetActive(bool active)
    {
        IsActive = active;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
