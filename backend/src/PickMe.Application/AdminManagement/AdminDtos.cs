using PickMe.Domain;

namespace PickMe.Application.AdminManagement;

// ---- Drivers ----
public sealed record CreateDriverDto(
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string? InitialPassword);

public sealed record UpdateDriverDto(
    string FirstName,
    string LastName,
    string Phone);

public sealed record DriverListItemDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    DriverStatus Status,
    decimal AverageRating,
    int TotalTrips,
    bool MustChangePassword,
    DateTime CreatedAtUtc);

public sealed record DriverDetailDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    DriverStatus Status,
    decimal AverageRating,
    int TotalTrips,
    bool MustChangePassword,
    DateTime CreatedAtUtc,
    int ActiveAssignmentCount,
    IReadOnlyList<RecentRatingDto> RecentRatings);

public sealed record RecentRatingDto(
    Guid ReservationId,
    int Score,
    string? Comment,
    DateTime CreatedAtUtc,
    bool IsFlagged);

// ---- Admin recipients ----
public sealed record CreateRecipientDto(string Email);
public sealed record RecipientDto(Guid Id, string Email, bool IsActive);

// ---- Customers (read-only) ----
public sealed record CustomerListItemDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    bool IsActive,
    int ReservationCount,
    DateTime CreatedAtUtc);

public sealed record CustomerDetailDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    bool IsActive,
    bool EmailConfirmed,
    int ReservationCount,
    DateTime CreatedAtUtc,
    IReadOnlyList<RecentReservationDto> RecentReservations);

public sealed record RecentReservationDto(
    Guid Id,
    ReservationStatus Status,
    DateTime ReservationDateTimeUtc,
    string Address);

// ---- FAQs ----
public sealed record CreateFaqDto(string Question, string Answer, int DisplayOrder);
public sealed record UpdateFaqDto(string Question, string Answer, int DisplayOrder, bool IsActive);
public sealed record FaqAdminDto(Guid Id, string Question, string Answer, int DisplayOrder, bool IsActive, DateTime CreatedAtUtc);

// ---- Contact messages ----
public sealed record ContactMessageDto(
    Guid Id,
    string FirstName,
    string Email,
    string Phone,
    string Subject,
    string Message,
    bool IsRead,
    DateTime? ReadAtUtc,
    DateTime CreatedAtUtc);

// ---- Ratings (admin) ----
public sealed record AdminRatingListItemDto(
    Guid Id,
    Guid ReservationId,
    int Score,
    string? Comment,
    string CustomerName,
    string DriverName,
    bool IsFlagged,
    string? FlaggedReason,
    DateTime CreatedAtUtc);

public sealed record FlagRatingDto(string Reason);

// ---- Admins (meta) ----
public sealed record CreateAdminDto(string FullName, string Email, string Password);
public sealed record UpdateAdminDto(string FullName);
public sealed record AdminUserDto(Guid Id, string FullName, string Email, DateTime CreatedAtUtc);

// ---- System settings ----
public sealed record SystemSettingDto(string Key, string Value, bool IsSensitive);
public sealed record UpdateSystemSettingsDto(IReadOnlyDictionary<string, string> Values);
