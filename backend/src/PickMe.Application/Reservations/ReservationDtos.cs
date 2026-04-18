using PickMe.Domain;

namespace PickMe.Application.Reservations;

// ---------- Input DTOs ----------

public sealed record CreateReservationDto(
    ServiceType ServiceType,
    DateTime ReservationDateTimeUtc,
    string Address,
    double Lat,
    double Lng,
    string? Note,
    bool PlaceSelectedFromAutocomplete);

public sealed record AssignDriverDto(Guid DriverId);
public sealed record CancelReservationDto(string? Reason);
public sealed record RateReservationDto(int Score, string? Comment);

public sealed record ReservationListQuery(
    ReservationStatus? Status,
    DateTime? DateFromUtc,
    DateTime? DateToUtc,
    Guid? CustomerId,
    Guid? DriverId,
    string? Search,
    int Page = 1,
    int PageSize = 20);

// ---------- Output DTOs ----------

public sealed record ReservationSummaryDto(
    Guid Id,
    ReservationStatus Status,
    ServiceType ServiceType,
    DateTime ReservationDateTimeUtc,
    string Address,
    double Lat,
    double Lng,
    string? Note,
    DateTime CreatedAtUtc);

public sealed record ReservationDetailDto(
    Guid Id,
    ReservationStatus Status,
    ServiceType ServiceType,
    DateTime ReservationDateTimeUtc,
    string Address,
    double Lat,
    double Lng,
    string? Note,
    Guid CustomerId,
    string CustomerName,
    string CustomerPhone,
    string CustomerEmail,
    Guid? DriverId,
    string? DriverName,
    string? DriverPhone,
    decimal? DriverAverageRating,
    string? CancellationReason,
    CancelledBy? CancelledBy,
    DateTime? AssignedAtUtc,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc,
    DateTime? CancelledAtUtc,
    DateTime CreatedAtUtc,
    bool HasRating,
    int? RatingScore,
    bool RatingEditable);

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}

public sealed record DriverSummaryDto(
    Guid Id,
    string FirstName,
    string LastName,
    string PhoneNumber,
    decimal AverageRating,
    int TotalTrips,
    DriverStatus Status);
