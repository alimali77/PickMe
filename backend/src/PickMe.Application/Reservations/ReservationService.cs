using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PickMe.Application.Abstractions;
using PickMe.Application.Auth;
using PickMe.Application.Common;
using PickMe.Domain;
using PickMe.Domain.Common;
using PickMe.Domain.Entities;

namespace PickMe.Application.Reservations;

public interface IReservationService
{
    Task<Result<Guid>> CreateAsync(Guid customerUserId, CreateReservationDto dto, CancellationToken ct);
    Task<Result<PagedResult<ReservationSummaryDto>>> ListOwnAsync(Guid customerUserId, ReservationStatus? status, CancellationToken ct);
    Task<Result<ReservationDetailDto>> GetOwnDetailAsync(Guid customerUserId, Guid reservationId, CancellationToken ct);
    Task<Result<Unit>> CancelByCustomerAsync(Guid customerUserId, Guid reservationId, CancellationToken ct);

    Task<Result<PagedResult<ReservationDetailDto>>> AdminListAsync(ReservationListQuery query, CancellationToken ct);
    Task<Result<ReservationDetailDto>> AdminGetDetailAsync(Guid reservationId, CancellationToken ct);
    Task<Result<Unit>> AdminAssignAsync(Guid reservationId, AssignDriverDto dto, CancellationToken ct);
    Task<Result<Unit>> AdminCancelAsync(Guid reservationId, CancelReservationDto dto, CancellationToken ct);
    Task<Result<IReadOnlyList<DriverSummaryDto>>> ListActiveDriversAsync(CancellationToken ct);

    Task<Result<PagedResult<ReservationDetailDto>>> DriverListOwnTasksAsync(Guid driverUserId, CancellationToken ct);
    Task<Result<ReservationDetailDto>> DriverGetOwnTaskAsync(Guid driverUserId, Guid reservationId, CancellationToken ct);
    Task<Result<Unit>> DriverStartAsync(Guid driverUserId, Guid reservationId, CancellationToken ct);
    Task<Result<Unit>> DriverCompleteAsync(Guid driverUserId, Guid reservationId, CancellationToken ct);
}

public sealed class ReservationService(
    IApplicationDbContext db,
    IEmailQueue emails,
    IClock clock,
    IValidator<CreateReservationDto> createValidator,
    IValidator<AssignDriverDto> assignValidator,
    AdminCancelReservationValidator adminCancelValidator,
    ILogger<ReservationService> logger) : IReservationService
{
    private readonly IApplicationDbContext _db = db;
    private readonly IEmailQueue _emails = emails;
    private readonly IClock _clock = clock;
    private readonly IValidator<CreateReservationDto> _createValidator = createValidator;
    private readonly IValidator<AssignDriverDto> _assignValidator = assignValidator;
    private readonly AdminCancelReservationValidator _adminCancelValidator = adminCancelValidator;
    private readonly ILogger<ReservationService> _logger = logger;

    // ----------------- Customer -----------------

    public async Task<Result<Guid>> CreateAsync(Guid customerUserId, CreateReservationDto dto, CancellationToken ct)
    {
        var validation = await _createValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid) return Validation<Guid>(validation);

        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.UserId == customerUserId, ct);
        if (customer is null) return Result<Guid>.Fail("auth.profile_missing", "Müşteri profili bulunamadı.");

        // En az 1 aktif admin bildirim alıcısı olmalı — brief gereği.
        var hasActiveRecipient = await _db.AdminNotificationRecipients.AnyAsync(r => r.IsActive, ct);
        if (!hasActiveRecipient)
        {
            _logger.LogWarning("Reservation create blocked: no active admin notification recipients.");
            return Result<Guid>.Fail("reservation.no_recipients", ValidationMessages.NoActiveRecipients);
        }

        var reservation = Reservation.Create(
            Guid.NewGuid(),
            customer.Id,
            dto.ServiceType,
            dto.ReservationDateTimeUtc,
            dto.Address,
            dto.Lat,
            dto.Lng,
            dto.Note);

        _db.Reservations.Add(reservation);
        await _db.SaveChangesAsync(ct);

        await NotifyAdminsOfNewReservationAsync(reservation, customer, ct);

        _logger.LogInformation("Reservation {Id} created by customer {CustomerId}", reservation.Id, customer.Id);
        return Result<Guid>.Ok(reservation.Id);
    }

    public async Task<Result<PagedResult<ReservationSummaryDto>>> ListOwnAsync(Guid customerUserId, ReservationStatus? status, CancellationToken ct)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.UserId == customerUserId, ct);
        if (customer is null) return Result<PagedResult<ReservationSummaryDto>>.Fail("auth.profile_missing", ValidationMessages.NotAuthenticated);

        var q = _db.Reservations.AsNoTracking().Where(r => r.CustomerId == customer.Id);
        if (status.HasValue) q = q.Where(r => r.Status == status.Value);

        var items = await q.OrderByDescending(r => r.ReservationDateTimeUtc)
            .Select(r => new ReservationSummaryDto(
                r.Id, r.Status, r.ServiceType, r.ReservationDateTimeUtc,
                r.Address, r.Lat, r.Lng, r.Note, r.CreatedAtUtc))
            .ToListAsync(ct);

        return Result<PagedResult<ReservationSummaryDto>>.Ok(new PagedResult<ReservationSummaryDto>(items, items.Count, 1, items.Count));
    }

    public async Task<Result<ReservationDetailDto>> GetOwnDetailAsync(Guid customerUserId, Guid reservationId, CancellationToken ct)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.UserId == customerUserId, ct);
        if (customer is null) return Result<ReservationDetailDto>.Fail("auth.profile_missing", ValidationMessages.NotAuthenticated);

        var r = await _db.Reservations.FirstOrDefaultAsync(x => x.Id == reservationId, ct);
        if (r is null || r.CustomerId != customer.Id)
        {
            // 404 even if someone else's reservation — IDOR engeli
            return Result<ReservationDetailDto>.Fail("reservation.not_found", ValidationMessages.ReservationNotFound);
        }
        return Result<ReservationDetailDto>.Ok(await MapDetailAsync(r, ct));
    }

    public async Task<Result<Unit>> CancelByCustomerAsync(Guid customerUserId, Guid reservationId, CancellationToken ct)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.UserId == customerUserId, ct);
        if (customer is null) return Result<Unit>.Fail("auth.profile_missing", ValidationMessages.NotAuthenticated);

        var r = await _db.Reservations.FirstOrDefaultAsync(x => x.Id == reservationId, ct);
        if (r is null || r.CustomerId != customer.Id)
        {
            return Result<Unit>.Fail("reservation.not_found", ValidationMessages.ReservationNotFound);
        }

        try { r.CancelByCustomer(); }
        catch (InvalidStateTransitionException ex)
        {
            return Result<Unit>.Fail("reservation.invalid_transition", ex.Message);
        }

        await _db.SaveChangesAsync(ct);
        await NotifyAdminsOfCustomerCancelAsync(r, customer, ct);
        return Result<Unit>.Ok(Unit.Value);
    }

    // ----------------- Admin -----------------

    public async Task<Result<PagedResult<ReservationDetailDto>>> AdminListAsync(ReservationListQuery q, CancellationToken ct)
    {
        var page = Math.Max(1, q.Page);
        var size = Math.Clamp(q.PageSize, 1, 100);

        var query = _db.Reservations.AsNoTracking();
        if (q.Status.HasValue) query = query.Where(r => r.Status == q.Status.Value);
        if (q.DateFromUtc.HasValue) query = query.Where(r => r.ReservationDateTimeUtc >= q.DateFromUtc.Value);
        if (q.DateToUtc.HasValue) query = query.Where(r => r.ReservationDateTimeUtc <= q.DateToUtc.Value);
        if (q.CustomerId.HasValue) query = query.Where(r => r.CustomerId == q.CustomerId.Value);
        if (q.DriverId.HasValue) query = query.Where(r => r.DriverId == q.DriverId.Value);
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var term = q.Search.Trim().ToLower();
            query = query.Where(r => r.Address.ToLower().Contains(term)
                                     || (r.Note != null && r.Note.ToLower().Contains(term)));
        }

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(r => r.CreatedAtUtc)
            .Skip((page - 1) * size).Take(size)
            .ToListAsync(ct);

        var detailed = new List<ReservationDetailDto>(items.Count);
        foreach (var r in items) detailed.Add(await MapDetailAsync(r, ct));

        return Result<PagedResult<ReservationDetailDto>>.Ok(new PagedResult<ReservationDetailDto>(detailed, total, page, size));
    }

    public async Task<Result<ReservationDetailDto>> AdminGetDetailAsync(Guid reservationId, CancellationToken ct)
    {
        var r = await _db.Reservations.FirstOrDefaultAsync(x => x.Id == reservationId, ct);
        if (r is null) return Result<ReservationDetailDto>.Fail("reservation.not_found", ValidationMessages.ReservationNotFound);
        return Result<ReservationDetailDto>.Ok(await MapDetailAsync(r, ct));
    }

    public async Task<Result<Unit>> AdminAssignAsync(Guid reservationId, AssignDriverDto dto, CancellationToken ct)
    {
        var validation = await _assignValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid) return Validation<Unit>(validation);

        var r = await _db.Reservations.FirstOrDefaultAsync(x => x.Id == reservationId, ct);
        if (r is null) return Result<Unit>.Fail("reservation.not_found", ValidationMessages.ReservationNotFound);

        var driver = await _db.Drivers.FirstOrDefaultAsync(d => d.Id == dto.DriverId, ct);
        if (driver is null) return Result<Unit>.Fail("driver.not_found", ValidationMessages.DriverNotFound);
        if (driver.Status != DriverStatus.Active) return Result<Unit>.Fail("driver.inactive", ValidationMessages.DriverInactive);

        try { r.AssignDriver(driver.Id); }
        catch (InvalidStateTransitionException ex)
        {
            return Result<Unit>.Fail("reservation.invalid_transition", ex.Message);
        }

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<Unit>.Fail("reservation.concurrency", "Bu rezervasyon başka bir yönetici tarafından güncellendi. Lütfen sayfayı yenileyiniz.");
        }

        await NotifyAssignmentAsync(r, driver, ct);
        return Result<Unit>.Ok(Unit.Value);
    }

    public async Task<Result<Unit>> AdminCancelAsync(Guid reservationId, CancelReservationDto dto, CancellationToken ct)
    {
        var validation = await _adminCancelValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid) return Validation<Unit>(validation);

        var r = await _db.Reservations.FirstOrDefaultAsync(x => x.Id == reservationId, ct);
        if (r is null) return Result<Unit>.Fail("reservation.not_found", ValidationMessages.ReservationNotFound);

        try { r.CancelByAdmin(dto.Reason!); }
        catch (InvalidStateTransitionException ex)
        {
            return Result<Unit>.Fail("reservation.invalid_transition", ex.Message);
        }
        catch (DomainException ex) when (ex.Code == "reservation.cancel_reason_required")
        {
            return Result<Unit>.Fail(ex.Code, ex.Message);
        }

        await _db.SaveChangesAsync(ct);
        await NotifyAdminCancelAsync(r, dto.Reason!, ct);
        return Result<Unit>.Ok(Unit.Value);
    }

    public async Task<Result<IReadOnlyList<DriverSummaryDto>>> ListActiveDriversAsync(CancellationToken ct)
    {
        var drivers = await _db.Drivers
            .AsNoTracking()
            .Where(d => d.Status == DriverStatus.Active)
            .OrderByDescending(d => d.AverageRating)
            .ThenBy(d => d.FirstName)
            .Select(d => new DriverSummaryDto(d.Id, d.FirstName, d.LastName, d.PhoneNumber, d.AverageRating, d.TotalTrips, d.Status))
            .ToListAsync(ct);
        return Result<IReadOnlyList<DriverSummaryDto>>.Ok(drivers);
    }

    // ----------------- Driver -----------------

    public async Task<Result<PagedResult<ReservationDetailDto>>> DriverListOwnTasksAsync(Guid driverUserId, CancellationToken ct)
    {
        var driver = await _db.Drivers.IgnoreQueryFilters().FirstOrDefaultAsync(d => d.UserId == driverUserId, ct);
        if (driver is null) return Result<PagedResult<ReservationDetailDto>>.Fail("auth.profile_missing", ValidationMessages.NotAuthenticated);

        var list = await _db.Reservations
            .Where(r => r.DriverId == driver.Id && (r.Status == ReservationStatus.Assigned || r.Status == ReservationStatus.OnTheWay || r.Status == ReservationStatus.Completed))
            .OrderBy(r => r.Status == ReservationStatus.Completed ? 1 : 0)
            .ThenBy(r => r.ReservationDateTimeUtc)
            .Take(50)
            .ToListAsync(ct);

        var detailed = new List<ReservationDetailDto>(list.Count);
        foreach (var r in list) detailed.Add(await MapDetailAsync(r, ct));
        return Result<PagedResult<ReservationDetailDto>>.Ok(new PagedResult<ReservationDetailDto>(detailed, detailed.Count, 1, detailed.Count));
    }

    public async Task<Result<ReservationDetailDto>> DriverGetOwnTaskAsync(Guid driverUserId, Guid reservationId, CancellationToken ct)
    {
        var driver = await _db.Drivers.IgnoreQueryFilters().FirstOrDefaultAsync(d => d.UserId == driverUserId, ct);
        if (driver is null) return Result<ReservationDetailDto>.Fail("auth.profile_missing", ValidationMessages.NotAuthenticated);

        var r = await _db.Reservations.FirstOrDefaultAsync(x => x.Id == reservationId, ct);
        if (r is null || r.DriverId != driver.Id)
        {
            return Result<ReservationDetailDto>.Fail("reservation.not_found", ValidationMessages.ReservationNotFound);
        }
        return Result<ReservationDetailDto>.Ok(await MapDetailAsync(r, ct));
    }

    public async Task<Result<Unit>> DriverStartAsync(Guid driverUserId, Guid reservationId, CancellationToken ct)
    {
        var driver = await _db.Drivers.IgnoreQueryFilters().FirstOrDefaultAsync(d => d.UserId == driverUserId, ct);
        if (driver is null) return Result<Unit>.Fail("auth.profile_missing", ValidationMessages.NotAuthenticated);

        var r = await _db.Reservations.FirstOrDefaultAsync(x => x.Id == reservationId, ct);
        if (r is null || r.DriverId != driver.Id)
        {
            return Result<Unit>.Fail("reservation.not_found", ValidationMessages.ReservationNotFound);
        }

        try { r.StartTrip(); }
        catch (InvalidStateTransitionException ex)
        {
            return Result<Unit>.Fail("reservation.invalid_transition", ex.Message);
        }
        await _db.SaveChangesAsync(ct);
        return Result<Unit>.Ok(Unit.Value);
    }

    public async Task<Result<Unit>> DriverCompleteAsync(Guid driverUserId, Guid reservationId, CancellationToken ct)
    {
        var driver = await _db.Drivers.IgnoreQueryFilters().FirstOrDefaultAsync(d => d.UserId == driverUserId, ct);
        if (driver is null) return Result<Unit>.Fail("auth.profile_missing", ValidationMessages.NotAuthenticated);

        var r = await _db.Reservations.FirstOrDefaultAsync(x => x.Id == reservationId, ct);
        if (r is null || r.DriverId != driver.Id)
        {
            return Result<Unit>.Fail("reservation.not_found", ValidationMessages.ReservationNotFound);
        }

        try { r.CompleteTrip(); }
        catch (InvalidStateTransitionException ex)
        {
            return Result<Unit>.Fail("reservation.invalid_transition", ex.Message);
        }
        await _db.SaveChangesAsync(ct);
        await NotifyCompletionAsync(r, ct);
        return Result<Unit>.Ok(Unit.Value);
    }

    // ----------------- Helpers -----------------

    private async Task<ReservationDetailDto> MapDetailAsync(Reservation r, CancellationToken ct)
    {
        var customer = await _db.Customers.Include(c => c.User).AsNoTracking().FirstAsync(c => c.Id == r.CustomerId, ct);
        Driver? driver = r.DriverId.HasValue
            ? await _db.Drivers.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(d => d.Id == r.DriverId, ct)
            : null;
        var rating = await _db.Ratings.AsNoTracking().FirstOrDefaultAsync(x => x.ReservationId == r.Id, ct);
        var ratingEditable = rating is not null && _clock.UtcNow <= rating.CreatedAtUtc.AddHours(ValidationRules.RatingEditWindowHours);

        return new ReservationDetailDto(
            r.Id, r.Status, r.ServiceType, r.ReservationDateTimeUtc,
            r.Address, r.Lat, r.Lng, r.Note,
            customer.Id, $"{customer.FirstName} {customer.LastName}".Trim(), customer.PhoneNumber, customer.User.Email,
            driver?.Id,
            driver is null ? null : $"{driver.FirstName} {driver.LastName}".Trim(),
            driver?.PhoneNumber,
            driver?.AverageRating,
            r.CancellationReason, r.CancelledBy,
            r.AssignedAtUtc, r.StartedAtUtc, r.CompletedAtUtc, r.CancelledAtUtc, r.CreatedAtUtc,
            rating is not null, rating?.Score, ratingEditable);
    }

    // ---- Mail notifications ----

    private async Task NotifyAdminsOfNewReservationAsync(Reservation r, Customer customer, CancellationToken ct)
    {
        var recipients = await _db.AdminNotificationRecipients.Where(x => x.IsActive).Select(x => x.Email).ToListAsync(ct);
        if (recipients.Count == 0) return;

        var formatted = r.ReservationDateTimeUtc.ToString("u");
        foreach (var to in recipients)
        {
            await _emails.EnqueueAsync(new EmailMessage(
                To: to,
                Subject: "Yeni rezervasyon — Pick Me",
                HtmlBody: $"<p>Yeni rezervasyon alındı.</p><ul><li>Müşteri: {H(customer.FirstName)} {H(customer.LastName)} ({H(customer.PhoneNumber)})</li><li>Hizmet: {H(r.ServiceType.ToString())}</li><li>Zaman: {H(formatted)}</li><li>Adres: {H(r.Address)}</li>{(r.Note is null ? "" : $"<li>Not: {H(r.Note)}</li>")}</ul>",
                PlainBody: $"Yeni rezervasyon.\nMüşteri: {customer.FirstName} {customer.LastName} ({customer.PhoneNumber})\nHizmet: {r.ServiceType}\nZaman: {formatted}\nAdres: {r.Address}\n{(r.Note is null ? "" : $"Not: {r.Note}\n")}",
                TemplateKey: "reservation.new_admin"), ct);
        }
    }

    private async Task NotifyAssignmentAsync(Reservation r, Driver driver, CancellationToken ct)
    {
        var customer = await _db.Customers.Include(c => c.User).AsNoTracking().FirstAsync(c => c.Id == r.CustomerId, ct);
        var driverUser = await _db.Users.AsNoTracking().FirstAsync(u => u.Id == driver.UserId, ct);
        var timeStr = r.ReservationDateTimeUtc.ToString("u");

        await _emails.EnqueueAsync(new EmailMessage(
            To: customer.User.Email,
            Subject: "Şoförünüz atandı — Pick Me",
            HtmlBody: $"<p>Merhaba {H(customer.FirstName)},</p><p>Rezervasyonunuz için şoförümüz atandı:</p><ul><li>Şoför: {H(driver.FirstName)} {H(driver.LastName)}</li><li>Telefon: <a href=\"tel:{H(driver.PhoneNumber)}\">{H(driver.PhoneNumber)}</a></li><li>Zaman: {H(timeStr)}</li></ul>",
            PlainBody: $"Merhaba {customer.FirstName},\nŞoförünüz: {driver.FirstName} {driver.LastName} ({driver.PhoneNumber})\nZaman: {timeStr}",
            TemplateKey: "reservation.assigned_customer"), ct);

        await _emails.EnqueueAsync(new EmailMessage(
            To: driverUser.Email,
            Subject: "Yeni görev atandı — Pick Me",
            HtmlBody: $"<p>Merhaba {H(driver.FirstName)},</p><p>Yeni görev:</p><ul><li>Müşteri: {H(customer.FirstName)} {H(customer.LastName)} ({H(customer.PhoneNumber)})</li><li>Zaman: {H(timeStr)}</li><li>Adres: {H(r.Address)}</li>{(r.Note is null ? "" : $"<li>Not: {H(r.Note)}</li>")}</ul>",
            PlainBody: $"Yeni görev.\nMüşteri: {customer.FirstName} {customer.LastName} ({customer.PhoneNumber})\nZaman: {timeStr}\nAdres: {r.Address}",
            TemplateKey: "reservation.assigned_driver"), ct);
    }

    private async Task NotifyAdminsOfCustomerCancelAsync(Reservation r, Customer customer, CancellationToken ct)
    {
        var recipients = await _db.AdminNotificationRecipients.Where(x => x.IsActive).Select(x => x.Email).ToListAsync(ct);
        foreach (var to in recipients)
        {
            await _emails.EnqueueAsync(new EmailMessage(
                To: to,
                Subject: "Rezervasyon müşteri tarafından iptal edildi — Pick Me",
                HtmlBody: $"<p>{H(customer.FirstName)} {H(customer.LastName)} ({H(customer.PhoneNumber)}) rezervasyonunu iptal etti.</p><p>Adres: {H(r.Address)}<br/>Zaman: {H(r.ReservationDateTimeUtc.ToString("u"))}</p>",
                PlainBody: $"Müşteri iptal etti: {customer.FirstName} {customer.LastName} ({customer.PhoneNumber})\nAdres: {r.Address}\nZaman: {r.ReservationDateTimeUtc:u}",
                TemplateKey: "reservation.cancelled_by_customer"), ct);
        }
    }

    private async Task NotifyAdminCancelAsync(Reservation r, string reason, CancellationToken ct)
    {
        var customer = await _db.Customers.Include(c => c.User).AsNoTracking().FirstAsync(c => c.Id == r.CustomerId, ct);
        await _emails.EnqueueAsync(new EmailMessage(
            To: customer.User.Email,
            Subject: "Rezervasyonunuz iptal edildi — Pick Me",
            HtmlBody: $"<p>Merhaba {H(customer.FirstName)},</p><p>Rezervasyonunuz iptal edildi. Sebep: {H(reason)}</p>",
            PlainBody: $"Rezervasyon iptal edildi. Sebep: {reason}",
            TemplateKey: "reservation.cancelled_by_admin"), ct);

        if (r.DriverId.HasValue)
        {
            var driver = await _db.Drivers.IgnoreQueryFilters().Include(d => d.User).AsNoTracking().FirstAsync(d => d.Id == r.DriverId, ct);
            await _emails.EnqueueAsync(new EmailMessage(
                To: driver.User.Email,
                Subject: "Görev iptal edildi — Pick Me",
                HtmlBody: $"<p>Merhaba {H(driver.FirstName)},</p><p>Atanan görev iptal edildi.</p>",
                PlainBody: "Atanan görev iptal edildi.",
                TemplateKey: "reservation.cancelled_driver_notice"), ct);
        }
    }

    private async Task NotifyCompletionAsync(Reservation r, CancellationToken ct)
    {
        var customer = await _db.Customers.Include(c => c.User).AsNoTracking().FirstAsync(c => c.Id == r.CustomerId, ct);
        await _emails.EnqueueAsync(new EmailMessage(
            To: customer.User.Email,
            Subject: "Yolculuğunuz tamamlandı — Pick Me",
            HtmlBody: $"<p>Merhaba {H(customer.FirstName)},</p><p>Yolculuğunuz tamamlandı. Şoförünüzü değerlendirmek için <a href=\"/hesabim/rezervasyonlar/{r.Id}/degerlendir\">tıklayın</a>.</p>",
            PlainBody: $"Yolculuğunuz tamamlandı. Değerlendirme linki hesabımızda.",
            TemplateKey: "reservation.completed_rating_invite"), ct);
    }

    private static string H(string s) => System.Net.WebUtility.HtmlEncode(s);

    private static Result<T> Validation<T>(FluentValidation.Results.ValidationResult v)
    {
        var dict = v.Errors
            .GroupBy(e => Camel(e.PropertyName))
            .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
        return Result<T>.Fail("validation", "Doğrulama hatası.", dict);
    }

    private static string Camel(string s) => string.IsNullOrEmpty(s) ? s : char.ToLowerInvariant(s[0]) + s[1..];
}
