using PickMe.Domain.Common;

namespace PickMe.Domain.Entities;

public sealed class Reservation : Entity<Guid>
{
    public Guid CustomerId { get; private set; }
    public Guid? DriverId { get; private set; }
    public ServiceType ServiceType { get; private set; }
    public DateTime ReservationDateTimeUtc { get; private set; }
    public string Address { get; private set; } = default!;
    public double Lat { get; private set; }
    public double Lng { get; private set; }
    public string? Note { get; private set; }
    public ReservationStatus Status { get; private set; } = ReservationStatus.Pending;

    public string? CancellationReason { get; private set; }
    public CancelledBy? CancelledBy { get; private set; }

    public DateTime? AssignedAtUtc { get; private set; }
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }

    public byte[] RowVersion { get; private set; } = new byte[8];

    public Customer Customer { get; private set; } = default!;
    public Driver? Driver { get; private set; }

    private Reservation() { }

    public static Reservation Create(
        Guid id,
        Guid customerId,
        ServiceType serviceType,
        DateTime reservationUtc,
        string address,
        double lat,
        double lng,
        string? note)
    {
        return new Reservation
        {
            Id = id,
            CustomerId = customerId,
            ServiceType = serviceType,
            ReservationDateTimeUtc = reservationUtc,
            Address = address.Trim(),
            Lat = lat,
            Lng = lng,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            Status = ReservationStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    public void AssignDriver(Guid driverId)
    {
        if (Status != ReservationStatus.Pending && Status != ReservationStatus.Assigned)
        {
            throw new InvalidStateTransitionException(Status.ToString(), nameof(ReservationStatus.Assigned), nameof(AssignDriver));
        }

        DriverId = driverId;
        Status = ReservationStatus.Assigned;
        AssignedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void StartTrip()
    {
        if (Status != ReservationStatus.Assigned)
        {
            throw new InvalidStateTransitionException(Status.ToString(), nameof(ReservationStatus.OnTheWay), nameof(StartTrip));
        }

        Status = ReservationStatus.OnTheWay;
        StartedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void CompleteTrip()
    {
        if (Status != ReservationStatus.OnTheWay)
        {
            throw new InvalidStateTransitionException(Status.ToString(), nameof(ReservationStatus.Completed), nameof(CompleteTrip));
        }

        Status = ReservationStatus.Completed;
        CompletedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void CancelByCustomer()
    {
        if (Status != ReservationStatus.Pending)
        {
            throw new InvalidStateTransitionException(Status.ToString(), nameof(ReservationStatus.Cancelled), nameof(CancelByCustomer));
        }

        Status = ReservationStatus.Cancelled;
        CancelledBy = Domain.CancelledBy.Customer;
        CancellationReason = "Müşteri iptal etti.";
        CancelledAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void CancelByAdmin(string reason)
    {
        if (Status == ReservationStatus.Completed || Status == ReservationStatus.Cancelled)
        {
            throw new InvalidStateTransitionException(Status.ToString(), nameof(ReservationStatus.Cancelled), nameof(CancelByAdmin));
        }
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("reservation.cancel_reason_required", "İptal sebebi zorunludur.");
        }

        Status = ReservationStatus.Cancelled;
        CancelledBy = Domain.CancelledBy.Admin;
        CancellationReason = reason.Trim();
        CancelledAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
