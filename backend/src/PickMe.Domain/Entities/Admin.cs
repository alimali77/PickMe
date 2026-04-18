using PickMe.Domain.Common;

namespace PickMe.Domain.Entities;

public sealed class Admin : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public string FullName { get; private set; } = default!;

    public User User { get; private set; } = default!;

    private Admin() { }

    public static Admin Create(Guid id, Guid userId, string fullName)
    {
        return new Admin
        {
            Id = id,
            UserId = userId,
            FullName = fullName.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    public void UpdateFullName(string fullName)
    {
        FullName = fullName.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
