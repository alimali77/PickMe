using PickMe.Domain.Common;

namespace PickMe.Domain.Entities;

public sealed class Customer : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string PhoneNumber { get; private set; } = default!;
    public bool KvkkAccepted { get; private set; }
    public DateTime? KvkkAcceptedAtUtc { get; private set; }

    public User User { get; private set; } = default!;

    private Customer() { }

    public static Customer Create(
        Guid id,
        Guid userId,
        string firstName,
        string lastName,
        string phone,
        bool kvkkAccepted)
    {
        return new Customer
        {
            Id = id,
            UserId = userId,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            PhoneNumber = phone.Trim(),
            KvkkAccepted = kvkkAccepted,
            KvkkAcceptedAtUtc = kvkkAccepted ? DateTime.UtcNow : null,
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
}
