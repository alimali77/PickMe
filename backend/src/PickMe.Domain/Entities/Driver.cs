using PickMe.Domain.Common;

namespace PickMe.Domain.Entities;

public sealed class Driver : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string PhoneNumber { get; private set; } = default!;
    public DriverStatus Status { get; private set; } = DriverStatus.Active;
    public decimal AverageRating { get; private set; }
    public int TotalTrips { get; private set; }
    public bool MustChangePassword { get; private set; } = true;
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAtUtc { get; private set; }

    public User User { get; private set; } = default!;

    private Driver() { }

    public static Driver Create(Guid id, Guid userId, string firstName, string lastName, string phone)
    {
        return new Driver
        {
            Id = id,
            UserId = userId,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            PhoneNumber = phone.Trim(),
            Status = DriverStatus.Active,
            MustChangePassword = true,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    public void UpdateProfile(string firstName, string lastName, string phone)
    {
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        PhoneNumber = phone.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetStatus(DriverStatus status)
    {
        Status = status;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ClearMustChangePassword()
    {
        MustChangePassword = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RequirePasswordChange()
    {
        MustChangePassword = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAtUtc = DateTime.UtcNow;
        Status = DriverStatus.Inactive;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RecalculateRating(decimal average, int count)
    {
        AverageRating = Math.Round(average, 2, MidpointRounding.AwayFromZero);
        TotalTrips = count;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
