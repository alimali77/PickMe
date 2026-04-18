using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PickMe.Application.Abstractions;
using PickMe.Application.Auth;
using PickMe.Application.Common;
using PickMe.Application.Reservations;
using PickMe.Domain;
using PickMe.Domain.Entities;

namespace PickMe.Application.AdminManagement;

public interface IDriverManagementService
{
    Task<Result<PagedResult<DriverListItemDto>>> ListAsync(string? search, int page, int pageSize, CancellationToken ct);
    Task<Result<DriverDetailDto>> GetAsync(Guid id, CancellationToken ct);
    Task<Result<Guid>> CreateAsync(CreateDriverDto dto, CancellationToken ct);
    Task<Result<Unit>> UpdateAsync(Guid id, UpdateDriverDto dto, CancellationToken ct);
    Task<Result<Unit>> SetActiveAsync(Guid id, bool active, CancellationToken ct);
    Task<Result<Unit>> ResetPasswordAsync(Guid id, CancellationToken ct);
    Task<Result<Unit>> SoftDeleteAsync(Guid id, CancellationToken ct);
}

public sealed class DriverManagementService(
    IApplicationDbContext db,
    IPasswordHasher hasher,
    IEmailQueue emails,
    IValidator<CreateDriverDto> createValidator,
    IValidator<UpdateDriverDto> updateValidator,
    ILogger<DriverManagementService> logger) : IDriverManagementService
{
    private readonly IApplicationDbContext _db = db;
    private readonly IPasswordHasher _hasher = hasher;
    private readonly IEmailQueue _emails = emails;
    private readonly IValidator<CreateDriverDto> _createValidator = createValidator;
    private readonly IValidator<UpdateDriverDto> _updateValidator = updateValidator;
    private readonly ILogger<DriverManagementService> _logger = logger;

    public async Task<Result<PagedResult<DriverListItemDto>>> ListAsync(string? search, int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var q = _db.Drivers.AsNoTracking().Join(_db.Users.AsNoTracking(), d => d.UserId, u => u.Id, (d, u) => new { d, u });
        if (!string.IsNullOrWhiteSpace(search))
        {
            var t = search.Trim().ToLowerInvariant();
            q = q.Where(x => x.d.FirstName.ToLower().Contains(t)
                             || x.d.LastName.ToLower().Contains(t)
                             || x.d.PhoneNumber.Contains(t)
                             || x.u.Email.ToLower().Contains(t));
        }

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(x => x.d.CreatedAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new DriverListItemDto(
                x.d.Id, x.d.FirstName, x.d.LastName, x.u.Email, x.d.PhoneNumber,
                x.d.Status, x.d.AverageRating, x.d.TotalTrips, x.d.MustChangePassword, x.d.CreatedAtUtc))
            .ToListAsync(ct);

        return Result<PagedResult<DriverListItemDto>>.Ok(new PagedResult<DriverListItemDto>(items, total, page, pageSize));
    }

    public async Task<Result<DriverDetailDto>> GetAsync(Guid id, CancellationToken ct)
    {
        var driver = await _db.Drivers.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, ct);
        if (driver is null) return Result<DriverDetailDto>.Fail("driver.not_found", ValidationMessages.DriverNotFound);
        var user = await _db.Users.AsNoTracking().FirstAsync(u => u.Id == driver.UserId, ct);

        var active = await _db.Reservations.CountAsync(
            r => r.DriverId == driver.Id && (r.Status == ReservationStatus.Assigned || r.Status == ReservationStatus.OnTheWay), ct);

        var recent = await _db.Ratings.AsNoTracking()
            .Where(r => r.DriverId == driver.Id)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Take(5)
            .Select(r => new RecentRatingDto(r.ReservationId, r.Score, r.Comment, r.CreatedAtUtc, r.IsFlagged))
            .ToListAsync(ct);

        return Result<DriverDetailDto>.Ok(new DriverDetailDto(
            driver.Id, driver.FirstName, driver.LastName, user.Email, driver.PhoneNumber,
            driver.Status, driver.AverageRating, driver.TotalTrips, driver.MustChangePassword,
            driver.CreatedAtUtc, active, recent));
    }

    public async Task<Result<Guid>> CreateAsync(CreateDriverDto dto, CancellationToken ct)
    {
        var validation = await _createValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid) return Validation<Guid>(validation);

        var email = dto.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email, ct))
        {
            return Result<Guid>.Fail("auth.email_taken", ValidationMessages.EmailAlreadyRegistered,
                new Dictionary<string, string[]> { ["email"] = [ValidationMessages.EmailAlreadyRegistered] });
        }

        var password = !string.IsNullOrEmpty(dto.InitialPassword) ? dto.InitialPassword : GenerateTempPassword();
        var userId = Guid.NewGuid();
        var user = User.Create(userId, email, _hasher.Hash(password), UserRole.Driver);
        user.ConfirmEmail(); // Admin tarafından oluşturulduğu için email confirmed kabul edilir.
        var driver = Driver.Create(Guid.NewGuid(), userId, dto.FirstName, dto.LastName, dto.Phone);

        _db.Users.Add(user);
        _db.Drivers.Add(driver);
        await _db.SaveChangesAsync(ct);

        await _emails.EnqueueAsync(new EmailMessage(
            To: email,
            Subject: "Pick Me – Şoför hesabınız oluşturuldu",
            HtmlBody: $"<p>Merhaba {H(dto.FirstName)},</p><p>Pick Me şoför hesabınız oluşturuldu.</p><ul><li>E-posta: <b>{H(email)}</b></li><li>Başlangıç şifresi: <b>{H(password)}</b></li></ul><p>İlk girişte şifrenizi değiştirmeniz istenecektir.</p>",
            PlainBody: $"Merhaba {dto.FirstName},\nŞoför hesabınız oluşturuldu.\nE-posta: {email}\nŞifre: {password}\nİlk girişte şifrenizi değiştirin.",
            TemplateKey: "driver.account_created"), ct);

        _logger.LogInformation("Driver {DriverId} created: {Email}", driver.Id, email);
        return Result<Guid>.Ok(driver.Id);
    }

    public async Task<Result<Unit>> UpdateAsync(Guid id, UpdateDriverDto dto, CancellationToken ct)
    {
        var validation = await _updateValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid) return Validation<Unit>(validation);

        var driver = await _db.Drivers.IgnoreQueryFilters().FirstOrDefaultAsync(d => d.Id == id, ct);
        if (driver is null) return Result<Unit>.Fail("driver.not_found", ValidationMessages.DriverNotFound);

        driver.UpdateProfile(dto.FirstName, dto.LastName, dto.Phone);
        await _db.SaveChangesAsync(ct);
        return Result<Unit>.Ok(Unit.Value);
    }

    public async Task<Result<Unit>> SetActiveAsync(Guid id, bool active, CancellationToken ct)
    {
        var driver = await _db.Drivers.IgnoreQueryFilters().FirstOrDefaultAsync(d => d.Id == id, ct);
        if (driver is null) return Result<Unit>.Fail("driver.not_found", ValidationMessages.DriverNotFound);

        if (!active)
        {
            var activeCount = await _db.Reservations.CountAsync(
                r => r.DriverId == driver.Id && (r.Status == ReservationStatus.Assigned || r.Status == ReservationStatus.OnTheWay), ct);
            if (activeCount > 0)
            {
                return Result<Unit>.Fail("driver.has_active_assignments",
                    $"Şoförün {activeCount} aktif görevi var, önce tamamlanması veya yeniden atanması gerekir.");
            }
        }

        driver.SetStatus(active ? DriverStatus.Active : DriverStatus.Inactive);
        await _db.SaveChangesAsync(ct);
        return Result<Unit>.Ok(Unit.Value);
    }

    public async Task<Result<Unit>> ResetPasswordAsync(Guid id, CancellationToken ct)
    {
        var driver = await _db.Drivers.IgnoreQueryFilters().FirstOrDefaultAsync(d => d.Id == id, ct);
        if (driver is null) return Result<Unit>.Fail("driver.not_found", ValidationMessages.DriverNotFound);
        var user = await _db.Users.FirstAsync(u => u.Id == driver.UserId, ct);

        var newPassword = GenerateTempPassword();
        user.UpdatePasswordHash(_hasher.Hash(newPassword));
        driver.RequirePasswordChange();

        var activeRefresh = await _db.RefreshTokens.Where(t => t.UserId == user.Id && t.RevokedAtUtc == null).ToListAsync(ct);
        foreach (var rt in activeRefresh) rt.Revoke(null);

        await _db.SaveChangesAsync(ct);

        await _emails.EnqueueAsync(new EmailMessage(
            To: user.Email,
            Subject: "Pick Me – Şifreniz sıfırlandı",
            HtmlBody: $"<p>Merhaba {H(driver.FirstName)},</p><p>Şifreniz yönetici tarafından sıfırlandı.</p><p>Yeni şifre: <b>{H(newPassword)}</b></p><p>Lütfen ilk girişte şifrenizi değiştirin.</p>",
            PlainBody: $"Şifreniz sıfırlandı. Yeni şifre: {newPassword}",
            TemplateKey: "driver.password_reset_by_admin"), ct);

        return Result<Unit>.Ok(Unit.Value);
    }

    public async Task<Result<Unit>> SoftDeleteAsync(Guid id, CancellationToken ct)
    {
        var driver = await _db.Drivers.IgnoreQueryFilters().FirstOrDefaultAsync(d => d.Id == id, ct);
        if (driver is null) return Result<Unit>.Fail("driver.not_found", ValidationMessages.DriverNotFound);

        var activeCount = await _db.Reservations.CountAsync(
            r => r.DriverId == driver.Id && (r.Status == ReservationStatus.Assigned || r.Status == ReservationStatus.OnTheWay), ct);
        if (activeCount > 0)
        {
            return Result<Unit>.Fail("driver.has_active_assignments",
                $"Şoförün {activeCount} aktif görevi var, önce tamamlanması veya yeniden atanması gerekir.");
        }

        driver.SoftDelete();
        // Kullanıcının da girişini kapat
        var user = await _db.Users.FirstAsync(u => u.Id == driver.UserId, ct);
        user.SetActive(false);

        var activeRefresh = await _db.RefreshTokens.Where(t => t.UserId == user.Id && t.RevokedAtUtc == null).ToListAsync(ct);
        foreach (var rt in activeRefresh) rt.Revoke(null);

        await _db.SaveChangesAsync(ct);
        return Result<Unit>.Ok(Unit.Value);
    }

    private static string GenerateTempPassword()
    {
        const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string Lower = "abcdefghjkmnpqrstuvwxyz";
        const string Digit = "23456789";
        const string Special = "!@#$%";
        var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        char Pick(string pool)
        {
            var buf = new byte[4];
            rng.GetBytes(buf);
            return pool[(int)(BitConverter.ToUInt32(buf, 0) % (uint)pool.Length)];
        }
        var chars = new[] { Pick(Upper), Pick(Lower), Pick(Digit), Pick(Special), Pick(Upper), Pick(Lower), Pick(Digit), Pick(Upper + Lower + Digit), Pick(Upper + Lower + Digit), Pick(Upper + Lower + Digit) };
        // karıştır (Fisher-Yates)
        for (int i = chars.Length - 1; i > 0; i--)
        {
            var buf = new byte[4];
            rng.GetBytes(buf);
            var j = (int)(BitConverter.ToUInt32(buf, 0) % (uint)(i + 1));
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }
        return new string(chars);
    }

    private static string H(string s) => System.Net.WebUtility.HtmlEncode(s);

    private static Result<T> Validation<T>(FluentValidation.Results.ValidationResult v)
    {
        var dict = v.Errors
            .GroupBy(e => string.IsNullOrEmpty(e.PropertyName) ? "_" : char.ToLowerInvariant(e.PropertyName[0]) + e.PropertyName[1..])
            .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
        return Result<T>.Fail("validation", "Doğrulama hatası.", dict);
    }
}

