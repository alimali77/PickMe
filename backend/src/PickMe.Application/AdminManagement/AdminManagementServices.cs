using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PickMe.Application.Abstractions;
using PickMe.Application.Auth;
using PickMe.Application.Common;
using PickMe.Application.Reservations;
using PickMe.Domain;
using PickMe.Domain.Entities;

namespace PickMe.Application.AdminManagement;

// ===================================================================
// Admin notification recipients — brief: en az 1 aktif kayıt zorunlu.
// ===================================================================

public interface IRecipientsService
{
    Task<Result<IReadOnlyList<RecipientDto>>> ListAsync(CancellationToken ct);
    Task<Result<Guid>> AddAsync(CreateRecipientDto dto, CancellationToken ct);
    Task<Result<Unit>> SetActiveAsync(Guid id, bool active, CancellationToken ct);
    Task<Result<Unit>> DeleteAsync(Guid id, CancellationToken ct);
}

public sealed class RecipientsService(
    IApplicationDbContext db,
    IValidator<CreateRecipientDto> createValidator) : IRecipientsService
{
    private readonly IApplicationDbContext _db = db;
    private readonly IValidator<CreateRecipientDto> _createValidator = createValidator;

    public async Task<Result<IReadOnlyList<RecipientDto>>> ListAsync(CancellationToken ct)
    {
        var items = await _db.AdminNotificationRecipients.AsNoTracking()
            .OrderBy(r => r.Email)
            .Select(r => new RecipientDto(r.Id, r.Email, r.IsActive))
            .ToListAsync(ct);
        return Result<IReadOnlyList<RecipientDto>>.Ok(items);
    }

    public async Task<Result<Guid>> AddAsync(CreateRecipientDto dto, CancellationToken ct)
    {
        var v = await _createValidator.ValidateAsync(dto, ct);
        if (!v.IsValid) return FromVal<Guid>(v);

        var email = dto.Email.Trim().ToLowerInvariant();
        if (await _db.AdminNotificationRecipients.AnyAsync(r => r.Email == email, ct))
        {
            return Result<Guid>.Fail("recipient.duplicate", "Bu e-posta zaten eklenmiş.");
        }
        var r = AdminNotificationRecipient.Create(Guid.NewGuid(), email);
        _db.AdminNotificationRecipients.Add(r);
        await _db.SaveChangesAsync(ct);
        return Result<Guid>.Ok(r.Id);
    }

    public async Task<Result<Unit>> SetActiveAsync(Guid id, bool active, CancellationToken ct)
    {
        var r = await _db.AdminNotificationRecipients.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (r is null) return Result<Unit>.Fail("recipient.not_found", "Alıcı bulunamadı.");

        if (!active)
        {
            var activeCount = await _db.AdminNotificationRecipients.CountAsync(x => x.IsActive && x.Id != id, ct);
            if (activeCount == 0)
            {
                return Result<Unit>.Fail("recipient.last_active",
                    "En az 1 aktif yönetici bildirim e-postası olmalıdır. Önce başka bir aktif kayıt ekleyiniz.");
            }
        }

        r.SetActive(active);
        await _db.SaveChangesAsync(ct);
        return Result<Unit>.Ok(Unit.Value);
    }

    public async Task<Result<Unit>> DeleteAsync(Guid id, CancellationToken ct)
    {
        var r = await _db.AdminNotificationRecipients.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (r is null) return Result<Unit>.Fail("recipient.not_found", "Alıcı bulunamadı.");

        if (r.IsActive)
        {
            var otherActive = await _db.AdminNotificationRecipients.CountAsync(x => x.IsActive && x.Id != id, ct);
            if (otherActive == 0)
            {
                return Result<Unit>.Fail("recipient.last_active",
                    "En az 1 aktif yönetici bildirim e-postası olmalıdır. Bu son aktif kayıt — silinemez.");
            }
        }

        // Remove
        (_db as DbContext)?.Remove(r);
        await _db.SaveChangesAsync(ct);
        return Result<Unit>.Ok(Unit.Value);
    }

    private static Result<T> FromVal<T>(FluentValidation.Results.ValidationResult v) => ValidationMap<T>(v);
    internal static Result<T> ValidationMap<T>(FluentValidation.Results.ValidationResult v)
    {
        var dict = v.Errors
            .GroupBy(e => string.IsNullOrEmpty(e.PropertyName) ? "_" : char.ToLowerInvariant(e.PropertyName[0]) + e.PropertyName[1..])
            .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
        return Result<T>.Fail("validation", "Doğrulama hatası.", dict);
    }
}

// ===================================================================
// FAQ yönetim servisi
// ===================================================================

public interface IFaqManagementService
{
    Task<Result<IReadOnlyList<FaqAdminDto>>> ListAsync(CancellationToken ct);
    Task<Result<Guid>> CreateAsync(CreateFaqDto dto, CancellationToken ct);
    Task<Result<Unit>> UpdateAsync(Guid id, UpdateFaqDto dto, CancellationToken ct);
    Task<Result<Unit>> DeleteAsync(Guid id, CancellationToken ct);
}

public sealed class FaqManagementService(
    IApplicationDbContext db,
    IValidator<CreateFaqDto> createValidator,
    IValidator<UpdateFaqDto> updateValidator) : IFaqManagementService
{
    private readonly IApplicationDbContext _db = db;
    private readonly IValidator<CreateFaqDto> _createValidator = createValidator;
    private readonly IValidator<UpdateFaqDto> _updateValidator = updateValidator;

    public async Task<Result<IReadOnlyList<FaqAdminDto>>> ListAsync(CancellationToken ct)
    {
        var items = await _db.Faqs.AsNoTracking()
            .OrderBy(f => f.DisplayOrder).ThenBy(f => f.CreatedAtUtc)
            .Select(f => new FaqAdminDto(f.Id, f.Question, f.Answer, f.DisplayOrder, f.IsActive, f.CreatedAtUtc))
            .ToListAsync(ct);
        return Result<IReadOnlyList<FaqAdminDto>>.Ok(items);
    }

    public async Task<Result<Guid>> CreateAsync(CreateFaqDto dto, CancellationToken ct)
    {
        var v = await _createValidator.ValidateAsync(dto, ct);
        if (!v.IsValid) return RecipientsService.ValidationMap<Guid>(v);

        var faq = Faq.Create(Guid.NewGuid(), dto.Question, dto.Answer, dto.DisplayOrder);
        _db.Faqs.Add(faq);
        await _db.SaveChangesAsync(ct);
        return Result<Guid>.Ok(faq.Id);
    }

    public async Task<Result<Unit>> UpdateAsync(Guid id, UpdateFaqDto dto, CancellationToken ct)
    {
        var v = await _updateValidator.ValidateAsync(dto, ct);
        if (!v.IsValid) return RecipientsService.ValidationMap<Unit>(v);

        var faq = await _db.Faqs.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (faq is null) return Result<Unit>.Fail("faq.not_found", "SSS bulunamadı.");

        faq.Update(dto.Question, dto.Answer, dto.DisplayOrder, dto.IsActive);
        await _db.SaveChangesAsync(ct);
        return Result<Unit>.Ok(Unit.Value);
    }

    public async Task<Result<Unit>> DeleteAsync(Guid id, CancellationToken ct)
    {
        var faq = await _db.Faqs.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (faq is null) return Result<Unit>.Fail("faq.not_found", "SSS bulunamadı.");
        (_db as DbContext)?.Remove(faq);
        await _db.SaveChangesAsync(ct);
        return Result<Unit>.Ok(Unit.Value);
    }
}

// ===================================================================
// Contact messages admin view
// ===================================================================

public interface IContactMessagesService
{
    Task<Result<PagedResult<ContactMessageDto>>> ListAsync(bool? unreadOnly, int page, int pageSize, CancellationToken ct);
    Task<Result<Unit>> MarkReadAsync(Guid id, CancellationToken ct);
}

public sealed class ContactMessagesService(IApplicationDbContext db) : IContactMessagesService
{
    private readonly IApplicationDbContext _db = db;

    public async Task<Result<PagedResult<ContactMessageDto>>> ListAsync(bool? unreadOnly, int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var q = _db.ContactMessages.AsNoTracking();
        if (unreadOnly == true) q = q.Where(m => !m.IsRead);

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(m => m.CreatedAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(m => new ContactMessageDto(m.Id, m.FirstName, m.Email, m.Phone, m.Subject, m.Message, m.IsRead, m.ReadAtUtc, m.CreatedAtUtc))
            .ToListAsync(ct);

        return Result<PagedResult<ContactMessageDto>>.Ok(new PagedResult<ContactMessageDto>(items, total, page, pageSize));
    }

    public async Task<Result<Unit>> MarkReadAsync(Guid id, CancellationToken ct)
    {
        var m = await _db.ContactMessages.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (m is null) return Result<Unit>.Fail("contact.not_found", "Mesaj bulunamadı.");
        if (!m.IsRead) { m.MarkRead(); await _db.SaveChangesAsync(ct); }
        return Result<Unit>.Ok(Unit.Value);
    }
}

// ===================================================================
// Customer admin view (read-only for Faz 5)
// ===================================================================

public interface ICustomerAdminService
{
    Task<Result<PagedResult<CustomerListItemDto>>> ListAsync(string? search, int page, int pageSize, CancellationToken ct);
    Task<Result<CustomerDetailDto>> GetAsync(Guid customerId, CancellationToken ct);
    Task<Result<Unit>> SetActiveAsync(Guid customerId, bool active, CancellationToken ct);
}

public sealed class CustomerAdminService(IApplicationDbContext db) : ICustomerAdminService
{
    private readonly IApplicationDbContext _db = db;

    public async Task<Result<PagedResult<CustomerListItemDto>>> ListAsync(string? search, int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var q = _db.Customers.AsNoTracking()
            .Join(_db.Users.AsNoTracking(), c => c.UserId, u => u.Id, (c, u) => new { c, u });

        if (!string.IsNullOrWhiteSpace(search))
        {
            var t = search.Trim().ToLowerInvariant();
            q = q.Where(x => x.c.FirstName.ToLower().Contains(t)
                             || x.c.LastName.ToLower().Contains(t)
                             || x.u.Email.ToLower().Contains(t)
                             || x.c.PhoneNumber.Contains(t));
        }

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(x => x.c.CreatedAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new CustomerListItemDto(
                x.c.Id, x.c.FirstName, x.c.LastName, x.u.Email, x.c.PhoneNumber,
                x.u.IsActive,
                _db.Reservations.Count(r => r.CustomerId == x.c.Id),
                x.c.CreatedAtUtc))
            .ToListAsync(ct);

        return Result<PagedResult<CustomerListItemDto>>.Ok(new PagedResult<CustomerListItemDto>(items, total, page, pageSize));
    }

    public async Task<Result<CustomerDetailDto>> GetAsync(Guid customerId, CancellationToken ct)
    {
        var c = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == customerId, ct);
        if (c is null) return Result<CustomerDetailDto>.Fail("customer.not_found", "Müşteri bulunamadı.");
        var u = await _db.Users.AsNoTracking().FirstAsync(x => x.Id == c.UserId, ct);

        var count = await _db.Reservations.CountAsync(r => r.CustomerId == c.Id, ct);
        var recent = await _db.Reservations.AsNoTracking()
            .Where(r => r.CustomerId == c.Id)
            .OrderByDescending(r => r.CreatedAtUtc).Take(10)
            .Select(r => new RecentReservationDto(r.Id, r.Status, r.ReservationDateTimeUtc, r.Address))
            .ToListAsync(ct);

        return Result<CustomerDetailDto>.Ok(new CustomerDetailDto(
            c.Id, c.FirstName, c.LastName, u.Email, c.PhoneNumber, u.IsActive, u.EmailConfirmed, count, c.CreatedAtUtc, recent));
    }

    public async Task<Result<Unit>> SetActiveAsync(Guid customerId, bool active, CancellationToken ct)
    {
        var c = await _db.Customers.FirstOrDefaultAsync(x => x.Id == customerId, ct);
        if (c is null) return Result<Unit>.Fail("customer.not_found", "Müşteri bulunamadı.");
        var u = await _db.Users.FirstAsync(x => x.Id == c.UserId, ct);
        u.SetActive(active);

        if (!active)
        {
            var activeRefresh = await _db.RefreshTokens.Where(t => t.UserId == u.Id && t.RevokedAtUtc == null).ToListAsync(ct);
            foreach (var rt in activeRefresh) rt.Revoke(null);
        }

        await _db.SaveChangesAsync(ct);
        return Result<Unit>.Ok(Unit.Value);
    }
}

// ===================================================================
// Ratings admin (list + flag/unflag + recompute driver avg)
// ===================================================================

public interface IRatingAdminService
{
    Task<Result<PagedResult<AdminRatingListItemDto>>> ListAsync(Guid? driverId, int? minScore, int? maxScore, int page, int pageSize, CancellationToken ct);
    Task<Result<Unit>> FlagAsync(Guid ratingId, FlagRatingDto dto, CancellationToken ct);
    Task<Result<Unit>> UnflagAsync(Guid ratingId, CancellationToken ct);
}

public sealed class RatingAdminService(
    IApplicationDbContext db,
    IValidator<FlagRatingDto> flagValidator) : IRatingAdminService
{
    private readonly IApplicationDbContext _db = db;
    private readonly IValidator<FlagRatingDto> _flagValidator = flagValidator;

    public async Task<Result<PagedResult<AdminRatingListItemDto>>> ListAsync(Guid? driverId, int? minScore, int? maxScore, int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var q = _db.Ratings.AsNoTracking()
            .Join(_db.Customers.AsNoTracking(), r => r.CustomerId, c => c.Id, (r, c) => new { r, c })
            .Join(_db.Drivers.IgnoreQueryFilters().AsNoTracking(), x => x.r.DriverId, d => d.Id, (x, d) => new { x.r, x.c, d });

        if (driverId.HasValue) q = q.Where(x => x.r.DriverId == driverId.Value);
        if (minScore.HasValue) q = q.Where(x => x.r.Score >= minScore.Value);
        if (maxScore.HasValue) q = q.Where(x => x.r.Score <= maxScore.Value);

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(x => x.r.CreatedAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new AdminRatingListItemDto(
                x.r.Id, x.r.ReservationId, x.r.Score, x.r.Comment,
                x.c.FirstName + " " + x.c.LastName,
                x.d.FirstName + " " + x.d.LastName,
                x.r.IsFlagged, x.r.FlaggedReason, x.r.CreatedAtUtc))
            .ToListAsync(ct);

        return Result<PagedResult<AdminRatingListItemDto>>.Ok(new PagedResult<AdminRatingListItemDto>(items, total, page, pageSize));
    }

    public async Task<Result<Unit>> FlagAsync(Guid ratingId, FlagRatingDto dto, CancellationToken ct)
    {
        var v = await _flagValidator.ValidateAsync(dto, ct);
        if (!v.IsValid) return RecipientsService.ValidationMap<Unit>(v);

        var rating = await _db.Ratings.FirstOrDefaultAsync(r => r.Id == ratingId, ct);
        if (rating is null) return Result<Unit>.Fail("rating.not_found", "Puan bulunamadı.");

        rating.Flag(dto.Reason);
        await _db.SaveChangesAsync(ct);
        await RecomputeDriverAvgAsync(rating.DriverId, ct);
        return Result<Unit>.Ok(Unit.Value);
    }

    public async Task<Result<Unit>> UnflagAsync(Guid ratingId, CancellationToken ct)
    {
        var rating = await _db.Ratings.FirstOrDefaultAsync(r => r.Id == ratingId, ct);
        if (rating is null) return Result<Unit>.Fail("rating.not_found", "Puan bulunamadı.");

        rating.Unflag();
        await _db.SaveChangesAsync(ct);
        await RecomputeDriverAvgAsync(rating.DriverId, ct);
        return Result<Unit>.Ok(Unit.Value);
    }

    private async Task RecomputeDriverAvgAsync(Guid driverId, CancellationToken ct)
    {
        var driver = await _db.Drivers.IgnoreQueryFilters().FirstOrDefaultAsync(d => d.Id == driverId, ct);
        if (driver is null) return;
        var scores = await _db.Ratings.Where(r => r.DriverId == driverId && !r.IsFlagged).Select(r => r.Score).ToListAsync(ct);
        if (scores.Count == 0) { driver.RecalculateRating(0m, 0); }
        else { driver.RecalculateRating((decimal)scores.Average(), scores.Count); }
        await _db.SaveChangesAsync(ct);
    }
}

// ===================================================================
// Admin users CRUD (self-protection: kendini ve son admini silemez)
// ===================================================================

public interface IAdminUsersService
{
    Task<Result<IReadOnlyList<AdminUserDto>>> ListAsync(CancellationToken ct);
    Task<Result<Guid>> CreateAsync(CreateAdminDto dto, CancellationToken ct);
    Task<Result<Unit>> UpdateAsync(Guid adminId, UpdateAdminDto dto, CancellationToken ct);
    Task<Result<Unit>> DeleteAsync(Guid adminId, Guid currentAdminUserId, CancellationToken ct);
}

public sealed class AdminUsersService(
    IApplicationDbContext db,
    IPasswordHasher hasher,
    IValidator<CreateAdminDto> createValidator,
    IValidator<UpdateAdminDto> updateValidator) : IAdminUsersService
{
    private readonly IApplicationDbContext _db = db;
    private readonly IPasswordHasher _hasher = hasher;
    private readonly IValidator<CreateAdminDto> _createValidator = createValidator;
    private readonly IValidator<UpdateAdminDto> _updateValidator = updateValidator;

    public async Task<Result<IReadOnlyList<AdminUserDto>>> ListAsync(CancellationToken ct)
    {
        var items = await _db.Admins.AsNoTracking()
            .Join(_db.Users.AsNoTracking(), a => a.UserId, u => u.Id, (a, u) => new AdminUserDto(a.Id, a.FullName, u.Email, a.CreatedAtUtc))
            .OrderBy(x => x.FullName)
            .ToListAsync(ct);
        return Result<IReadOnlyList<AdminUserDto>>.Ok(items);
    }

    public async Task<Result<Guid>> CreateAsync(CreateAdminDto dto, CancellationToken ct)
    {
        var v = await _createValidator.ValidateAsync(dto, ct);
        if (!v.IsValid) return RecipientsService.ValidationMap<Guid>(v);

        var email = dto.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email, ct))
        {
            return Result<Guid>.Fail("auth.email_taken", ValidationMessages.EmailAlreadyRegistered,
                new Dictionary<string, string[]> { ["email"] = [ValidationMessages.EmailAlreadyRegistered] });
        }

        var userId = Guid.NewGuid();
        var user = User.Create(userId, email, _hasher.Hash(dto.Password), UserRole.Admin);
        user.ConfirmEmail();
        var admin = Admin.Create(Guid.NewGuid(), userId, dto.FullName);
        _db.Users.Add(user);
        _db.Admins.Add(admin);
        await _db.SaveChangesAsync(ct);
        return Result<Guid>.Ok(admin.Id);
    }

    public async Task<Result<Unit>> UpdateAsync(Guid adminId, UpdateAdminDto dto, CancellationToken ct)
    {
        var v = await _updateValidator.ValidateAsync(dto, ct);
        if (!v.IsValid) return RecipientsService.ValidationMap<Unit>(v);

        var admin = await _db.Admins.FirstOrDefaultAsync(a => a.Id == adminId, ct);
        if (admin is null) return Result<Unit>.Fail("admin.not_found", "Yönetici bulunamadı.");
        admin.UpdateFullName(dto.FullName);
        await _db.SaveChangesAsync(ct);
        return Result<Unit>.Ok(Unit.Value);
    }

    public async Task<Result<Unit>> DeleteAsync(Guid adminId, Guid currentAdminUserId, CancellationToken ct)
    {
        var admin = await _db.Admins.FirstOrDefaultAsync(a => a.Id == adminId, ct);
        if (admin is null) return Result<Unit>.Fail("admin.not_found", "Yönetici bulunamadı.");

        if (admin.UserId == currentAdminUserId)
        {
            return Result<Unit>.Fail("admin.cannot_delete_self", "Kendi hesabınızı silemezsiniz.");
        }

        var total = await _db.Admins.CountAsync(ct);
        if (total <= 1)
        {
            return Result<Unit>.Fail("admin.last_admin", "Sistemde en az 1 yönetici olmalıdır.");
        }

        var user = await _db.Users.FirstAsync(u => u.Id == admin.UserId, ct);
        (_db as DbContext)?.Remove(admin);
        (_db as DbContext)?.Remove(user); // Cascade olmadığı için user'ı da kaldırıyoruz.
        await _db.SaveChangesAsync(ct);
        return Result<Unit>.Ok(Unit.Value);
    }
}

// ===================================================================
// System settings (key-value, bir kısmı sensitive)
// ===================================================================

public interface ISystemSettingsService
{
    Task<Result<IReadOnlyList<SystemSettingDto>>> ListAsync(CancellationToken ct);
    Task<Result<Unit>> UpdateAsync(UpdateSystemSettingsDto dto, CancellationToken ct);
    Task<Result<string?>> GetPublicAsync(string key, CancellationToken ct);
}

public sealed class SystemSettingsService(IApplicationDbContext db) : ISystemSettingsService
{
    private readonly IApplicationDbContext _db = db;

    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "smtp.password", "google.maps.api_key", "ga4.id",
    };

    private static readonly HashSet<string> PublicKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "whatsapp.number", "contact.email", "contact.phone", "contact.address",
        "working.hours", "ga4.id",
    };

    public async Task<Result<IReadOnlyList<SystemSettingDto>>> ListAsync(CancellationToken ct)
    {
        var items = await _db.SystemSettings.AsNoTracking()
            .OrderBy(s => s.Key)
            .Select(s => new SystemSettingDto(s.Key, s.IsSensitive ? Mask(s.Value) : s.Value, s.IsSensitive))
            .ToListAsync(ct);
        return Result<IReadOnlyList<SystemSettingDto>>.Ok(items);
    }

    public async Task<Result<Unit>> UpdateAsync(UpdateSystemSettingsDto dto, CancellationToken ct)
    {
        if (dto.Values is null || dto.Values.Count == 0) return Result<Unit>.Ok(Unit.Value);

        foreach (var (key, value) in dto.Values)
        {
            if (string.IsNullOrWhiteSpace(key)) continue;
            var existing = await _db.SystemSettings.FirstOrDefaultAsync(s => s.Key == key, ct);
            var isSensitive = SensitiveKeys.Contains(key);
            if (existing is null)
            {
                _db.SystemSettings.Add(SystemSetting.Create(Guid.NewGuid(), key, value ?? string.Empty, isSensitive));
            }
            else
            {
                existing.UpdateValue(value ?? string.Empty);
            }
        }
        await _db.SaveChangesAsync(ct);
        return Result<Unit>.Ok(Unit.Value);
    }

    public async Task<Result<string?>> GetPublicAsync(string key, CancellationToken ct)
    {
        if (!PublicKeys.Contains(key))
        {
            return Result<string?>.Fail("settings.not_public", "Bu ayar herkese açık değildir.");
        }
        var setting = await _db.SystemSettings.AsNoTracking().FirstOrDefaultAsync(s => s.Key == key, ct);
        return Result<string?>.Ok(setting?.Value);
    }

    private static string Mask(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Length <= 4) return new string('*', value.Length);
        return value[..2] + new string('*', Math.Min(value.Length - 4, 10)) + value[^2..];
    }
}
