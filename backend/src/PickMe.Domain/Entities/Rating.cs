using PickMe.Domain.Common;

namespace PickMe.Domain.Entities;

public sealed class Rating : Entity<Guid>
{
    public Guid ReservationId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid DriverId { get; private set; }
    public int Score { get; private set; }
    public string? Comment { get; private set; }
    public bool IsFlagged { get; private set; }
    public string? FlaggedReason { get; private set; }

    public Reservation Reservation { get; private set; } = default!;

    private Rating() { }

    public static Rating Create(
        Guid id,
        Guid reservationId,
        Guid customerId,
        Guid driverId,
        int score,
        string? comment)
    {
        ValidateScore(score);

        return new Rating
        {
            Id = id,
            ReservationId = reservationId,
            CustomerId = customerId,
            DriverId = driverId,
            Score = score,
            Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    public void Edit(int score, string? comment, DateTime nowUtc, int editWindowHours)
    {
        if (nowUtc > CreatedAtUtc.AddHours(editWindowHours))
        {
            throw new DomainException("rating.edit_window_expired", "Puan oluşturulduktan 24 saat sonra düzenlenemez.");
        }

        ValidateScore(score);
        Score = score;
        Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        UpdatedAtUtc = nowUtc;
    }

    public void Flag(string reason)
    {
        IsFlagged = true;
        FlaggedReason = reason;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Unflag()
    {
        IsFlagged = false;
        FlaggedReason = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static void ValidateScore(int score)
    {
        if (score < 1 || score > 5)
        {
            throw new DomainException("rating.score_out_of_range", "Puan 1 ile 5 arasında olmalıdır.");
        }
    }
}
