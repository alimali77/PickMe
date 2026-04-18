using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PickMe.Application.Abstractions;
using PickMe.Application.Auth;
using PickMe.Application.Common;
using PickMe.Domain;
using PickMe.Domain.Entities;

namespace PickMe.Application.Reservations;

public interface IRatingService
{
    Task<Result<Unit>> RateAsync(Guid customerUserId, Guid reservationId, RateReservationDto dto, CancellationToken ct);
    Task<Result<Unit>> EditAsync(Guid customerUserId, Guid reservationId, RateReservationDto dto, CancellationToken ct);
}

public sealed class RatingService(
    IApplicationDbContext db,
    IClock clock,
    IValidator<RateReservationDto> validator,
    ILogger<RatingService> logger) : IRatingService
{
    private readonly IApplicationDbContext _db = db;
    private readonly IClock _clock = clock;
    private readonly IValidator<RateReservationDto> _validator = validator;
    private readonly ILogger<RatingService> _logger = logger;

    public async Task<Result<Unit>> RateAsync(Guid customerUserId, Guid reservationId, RateReservationDto dto, CancellationToken ct)
    {
        var validation = await _validator.ValidateAsync(dto, ct);
        if (!validation.IsValid) return FromValidation(validation);

        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.UserId == customerUserId, ct);
        if (customer is null) return Result<Unit>.Fail("auth.profile_missing", ValidationMessages.NotAuthenticated);

        var reservation = await _db.Reservations.FirstOrDefaultAsync(r => r.Id == reservationId, ct);
        if (reservation is null || reservation.CustomerId != customer.Id)
        {
            return Result<Unit>.Fail("reservation.not_found", ValidationMessages.ReservationNotFound);
        }
        if (reservation.Status != ReservationStatus.Completed || !reservation.DriverId.HasValue)
        {
            return Result<Unit>.Fail("rating.not_completed", "Değerlendirme yalnızca tamamlanan rezervasyonlar için yapılabilir.");
        }

        var existing = await _db.Ratings.FirstOrDefaultAsync(x => x.ReservationId == reservationId, ct);
        if (existing is not null)
        {
            return Result<Unit>.Fail("rating.already_given", ValidationMessages.RatingAlreadyGiven);
        }

        var driverId = reservation.DriverId.Value;
        var rating = Rating.Create(Guid.NewGuid(), reservation.Id, customer.Id, driverId, dto.Score, dto.Comment);
        _db.Ratings.Add(rating);

        try { await _db.SaveChangesAsync(ct); }
        catch (DbUpdateException)
        {
            return Result<Unit>.Fail("rating.already_given", ValidationMessages.RatingAlreadyGiven);
        }

        await RecalculateDriverRatingAsync(driverId, ct);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Rating {RatingId} created for reservation {ReservationId}", rating.Id, reservation.Id);
        return Result<Unit>.Ok(Unit.Value);
    }

    public async Task<Result<Unit>> EditAsync(Guid customerUserId, Guid reservationId, RateReservationDto dto, CancellationToken ct)
    {
        var validation = await _validator.ValidateAsync(dto, ct);
        if (!validation.IsValid) return FromValidation(validation);

        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.UserId == customerUserId, ct);
        if (customer is null) return Result<Unit>.Fail("auth.profile_missing", ValidationMessages.NotAuthenticated);

        var rating = await _db.Ratings.FirstOrDefaultAsync(x => x.ReservationId == reservationId && x.CustomerId == customer.Id, ct);
        if (rating is null) return Result<Unit>.Fail("rating.not_found", "Düzenlenecek puan bulunamadı.");

        try { rating.Edit(dto.Score, dto.Comment, _clock.UtcNow, ValidationRules.RatingEditWindowHours); }
        catch (Domain.Common.DomainException ex) when (ex.Code == "rating.edit_window_expired")
        {
            return Result<Unit>.Fail(ex.Code, ex.Message);
        }

        await _db.SaveChangesAsync(ct);
        await RecalculateDriverRatingAsync(rating.DriverId, ct);
        await _db.SaveChangesAsync(ct);
        return Result<Unit>.Ok(Unit.Value);
    }

    private async Task RecalculateDriverRatingAsync(Guid driverId, CancellationToken ct)
    {
        var driver = await _db.Drivers.IgnoreQueryFilters().FirstOrDefaultAsync(d => d.Id == driverId, ct);
        if (driver is null) return;

        var scores = await _db.Ratings
            .Where(r => r.DriverId == driverId && !r.IsFlagged)
            .Select(r => r.Score)
            .ToListAsync(ct);

        if (scores.Count == 0)
        {
            driver.RecalculateRating(0m, 0);
            return;
        }
        var avg = (decimal)scores.Average();
        driver.RecalculateRating(avg, scores.Count);
    }

    private static Result<Unit> FromValidation(FluentValidation.Results.ValidationResult v)
    {
        var dict = v.Errors
            .GroupBy(e => string.IsNullOrEmpty(e.PropertyName) ? "_" : char.ToLowerInvariant(e.PropertyName[0]) + e.PropertyName[1..])
            .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
        return Result<Unit>.Fail("validation", "Doğrulama hatası.", dict);
    }
}
