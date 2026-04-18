using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PickMe.Application.Abstractions;
using PickMe.Application.Common;
using PickMe.Domain;
using PickMe.Domain.Entities;

namespace PickMe.Application.Auth;

public interface IAuthService
{
    Task<Result<Unit>> RegisterCustomerAsync(RegisterCustomerDto dto, CancellationToken ct);
    Task<Result<Unit>> VerifyEmailAsync(VerifyEmailDto dto, CancellationToken ct);
    Task<Result<Unit>> ResendVerificationAsync(ResendVerificationDto dto, CancellationToken ct);
    Task<Result<AuthTokensDto>> LoginAsync(LoginDto dto, CancellationToken ct);
    Task<Result<AuthTokensDto>> RefreshAsync(RefreshDto dto, CancellationToken ct);
    Task<Result<Unit>> LogoutAsync(LogoutDto dto, CancellationToken ct);
    Task<Result<Unit>> ForgotPasswordAsync(ForgotPasswordDto dto, CancellationToken ct);
    Task<Result<Unit>> ResetPasswordAsync(ResetPasswordDto dto, CancellationToken ct);
    Task<Result<Unit>> ChangePasswordAsync(Guid userId, ChangePasswordDto dto, CancellationToken ct);
    Task<Result<CurrentUserDto>> GetCurrentUserAsync(Guid userId, CancellationToken ct);
    Task<Result<CurrentUserDto>> UpdateProfileAsync(Guid userId, UpdateProfileDto dto, CancellationToken ct);
}

public sealed class AuthService(
    IApplicationDbContext db,
    IPasswordHasher hasher,
    ITokenHasher tokenHasher,
    IOpaqueTokenGenerator tokenGen,
    IJwtTokenService jwt,
    IEmailQueue emailQueue,
    IClock clock,
    IValidator<RegisterCustomerDto> registerValidator,
    IValidator<LoginDto> loginValidator,
    IValidator<ForgotPasswordDto> forgotValidator,
    IValidator<ResetPasswordDto> resetValidator,
    IValidator<ChangePasswordDto> changeValidator,
    IValidator<UpdateProfileDto> updateProfileValidator,
    IFrontendUrlProvider urls,
    ILogger<AuthService> logger) : IAuthService
{
    private readonly IApplicationDbContext _db = db;
    private readonly IPasswordHasher _hasher = hasher;
    private readonly ITokenHasher _tokenHasher = tokenHasher;
    private readonly IOpaqueTokenGenerator _tokenGen = tokenGen;
    private readonly IJwtTokenService _jwt = jwt;
    private readonly IEmailQueue _emailQueue = emailQueue;
    private readonly IClock _clock = clock;
    private readonly IFrontendUrlProvider _urls = urls;
    private readonly ILogger<AuthService> _logger = logger;
    private readonly IValidator<RegisterCustomerDto> _registerValidator = registerValidator;
    private readonly IValidator<LoginDto> _loginValidator = loginValidator;
    private readonly IValidator<ForgotPasswordDto> _forgotValidator = forgotValidator;
    private readonly IValidator<ResetPasswordDto> _resetValidator = resetValidator;
    private readonly IValidator<ChangePasswordDto> _changeValidator = changeValidator;
    private readonly IValidator<UpdateProfileDto> _updateProfileValidator = updateProfileValidator;

    // ------------- Register -------------

    public async Task<Result<Unit>> RegisterCustomerAsync(RegisterCustomerDto dto, CancellationToken ct)
    {
        var validation = await _registerValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid) return FromValidation<Unit>(validation);

        var email = dto.Email.Trim().ToLowerInvariant();
        var exists = await _db.Users.AnyAsync(u => u.Email == email, ct);
        if (exists)
        {
            return Result<Unit>.Fail("auth.email_taken", ValidationMessages.EmailAlreadyRegistered,
                new Dictionary<string, string[]> { ["email"] = [ValidationMessages.EmailAlreadyRegistered] });
        }

        var userId = Guid.NewGuid();
        var user = User.Create(userId, email, _hasher.Hash(dto.Password), UserRole.Customer);
        var customer = Customer.Create(Guid.NewGuid(), userId, dto.FirstName, dto.LastName, dto.Phone, dto.KvkkAccepted);

        var plainToken = _tokenGen.Generate();
        var token = EmailVerificationToken.Create(
            Guid.NewGuid(),
            userId,
            _tokenHasher.Hash(plainToken),
            _clock.UtcNow.AddHours(ValidationRules.EmailVerificationTokenHours));

        _db.Users.Add(user);
        _db.Customers.Add(customer);
        _db.EmailVerificationTokens.Add(token);
        await _db.SaveChangesAsync(ct);

        var verifyUrl = _urls.VerifyEmailUrl(plainToken);
        await _emailQueue.EnqueueAsync(new EmailMessage(
            To: email,
            Subject: "Pick Me – E-posta doğrulama",
            HtmlBody: $"<p>Merhaba {HtmlEncode(dto.FirstName)},</p><p>Hesabınızı doğrulamak için <a href=\"{verifyUrl}\">tıklayın</a>. Bağlantı 24 saat geçerlidir.</p>",
            PlainBody: $"Hesabınızı doğrulamak için: {verifyUrl}\nBağlantı 24 saat geçerlidir.",
            TemplateKey: "auth.verify_email"), ct);

        _logger.LogInformation("Customer registered: {Email}", email);
        return Result<Unit>.Ok(Unit.Value);
    }

    public async Task<Result<Unit>> VerifyEmailAsync(VerifyEmailDto dto, CancellationToken ct)
    {
        var hash = _tokenHasher.Hash(dto.Token);
        var token = await _db.EmailVerificationTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (token is null || !token.IsValid(_clock.UtcNow))
        {
            return Result<Unit>.Fail("auth.invalid_token", ValidationMessages.InvalidOrExpiredToken);
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == token.UserId, ct);
        if (user is null) return Result<Unit>.Fail("auth.invalid_token", ValidationMessages.InvalidOrExpiredToken);

        user.ConfirmEmail();
        token.MarkUsed();
        await _db.SaveChangesAsync(ct);
        return Result<Unit>.Ok(Unit.Value);
    }

    public async Task<Result<Unit>> ResendVerificationAsync(ResendVerificationDto dto, CancellationToken ct)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        // Enumeration'ı engellemek için başarılı/başarısız her durumda aynı generic cevap.
        if (user is null || user.EmailConfirmed) return Result<Unit>.Ok(Unit.Value);

        var plainToken = _tokenGen.Generate();
        var token = EmailVerificationToken.Create(
            Guid.NewGuid(),
            user.Id,
            _tokenHasher.Hash(plainToken),
            _clock.UtcNow.AddHours(ValidationRules.EmailVerificationTokenHours));
        _db.EmailVerificationTokens.Add(token);
        await _db.SaveChangesAsync(ct);

        var verifyUrl = _urls.VerifyEmailUrl(plainToken);
        await _emailQueue.EnqueueAsync(new EmailMessage(
            To: email,
            Subject: "Pick Me – E-posta doğrulama",
            HtmlBody: $"<p>Hesabınızı doğrulamak için <a href=\"{verifyUrl}\">tıklayın</a>. Bağlantı 24 saat geçerlidir.</p>",
            PlainBody: $"Doğrulama: {verifyUrl}",
            TemplateKey: "auth.verify_email_resend"), ct);
        return Result<Unit>.Ok(Unit.Value);
    }

    // ------------- Login / refresh / logout -------------

    public async Task<Result<AuthTokensDto>> LoginAsync(LoginDto dto, CancellationToken ct)
    {
        var validation = await _loginValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid) return FromValidation<AuthTokensDto>(validation);

        var email = dto.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        var genericFail = Result<AuthTokensDto>.Fail("auth.invalid_credentials", ValidationMessages.LoginInvalid);

        if (user is null) return genericFail;
        if (!user.IsActive) return Result<AuthTokensDto>.Fail("auth.inactive", ValidationMessages.AccountInactive);
        if (user.IsLocked(_clock.UtcNow)) return Result<AuthTokensDto>.Fail("auth.locked", ValidationMessages.AccountLocked);

        if (!_hasher.Verify(dto.Password, user.PasswordHash))
        {
            user.RecordFailedLogin(ValidationRules.LoginMaxFailedAttempts, ValidationRules.LoginLockoutMinutes);
            await _db.SaveChangesAsync(ct);
            return user.IsLocked(_clock.UtcNow)
                ? Result<AuthTokensDto>.Fail("auth.locked", ValidationMessages.AccountLocked)
                : genericFail;
        }

        if (!user.EmailConfirmed)
        {
            return Result<AuthTokensDto>.Fail("auth.email_not_verified", ValidationMessages.EmailNotVerified);
        }

        user.RecordSuccessfulLogin();
        var tokens = await IssueTokensAsync(user, ct);
        await _db.SaveChangesAsync(ct);
        return Result<AuthTokensDto>.Ok(tokens);
    }

    public async Task<Result<AuthTokensDto>> RefreshAsync(RefreshDto dto, CancellationToken ct)
    {
        var hash = _tokenHasher.Hash(dto.RefreshToken);
        var existing = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (existing is null || !existing.IsActive(_clock.UtcNow))
        {
            return Result<AuthTokensDto>.Fail("auth.invalid_refresh", ValidationMessages.InvalidOrExpiredToken);
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == existing.UserId, ct);
        if (user is null || !user.IsActive)
        {
            return Result<AuthTokensDto>.Fail("auth.invalid_refresh", ValidationMessages.InvalidOrExpiredToken);
        }

        // Rotation: eskiyi revoke et, yenisini oluştur.
        var (newPlain, newEntity) = CreateRefreshToken(user.Id);
        existing.Revoke(newEntity.Id);
        _db.RefreshTokens.Add(newEntity);

        var access = _jwt.CreateAccessToken(user.Id, user.Email, user.Role.ToString());
        await _db.SaveChangesAsync(ct);

        return Result<AuthTokensDto>.Ok(new AuthTokensDto(
            access.Token, newPlain, access.ExpiresAtUtc, newEntity.ExpiresAtUtc));
    }

    public async Task<Result<Unit>> LogoutAsync(LogoutDto dto, CancellationToken ct)
    {
        var hash = _tokenHasher.Hash(dto.RefreshToken);
        var existing = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (existing is not null && existing.IsActive(_clock.UtcNow))
        {
            existing.Revoke(null);
            await _db.SaveChangesAsync(ct);
        }
        return Result<Unit>.Ok(Unit.Value);
    }

    // ------------- Forgot / reset / change -------------

    public async Task<Result<Unit>> ForgotPasswordAsync(ForgotPasswordDto dto, CancellationToken ct)
    {
        var validation = await _forgotValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid) return FromValidation<Unit>(validation);

        var email = dto.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        // Enumeration engeli: mevcut/yok her durumda aynı yanıt.
        if (user is null) return Result<Unit>.Ok(Unit.Value);

        var plainToken = _tokenGen.Generate();
        var token = PasswordResetToken.Create(
            Guid.NewGuid(),
            user.Id,
            _tokenHasher.Hash(plainToken),
            _clock.UtcNow.AddMinutes(ValidationRules.PasswordResetTokenMinutes));
        _db.PasswordResetTokens.Add(token);
        await _db.SaveChangesAsync(ct);

        var resetUrl = _urls.ResetPasswordUrl(plainToken);
        await _emailQueue.EnqueueAsync(new EmailMessage(
            To: email,
            Subject: "Pick Me – Şifre sıfırlama",
            HtmlBody: $"<p>Şifrenizi sıfırlamak için <a href=\"{resetUrl}\">tıklayın</a>. Bağlantı 1 saat geçerlidir.</p>",
            PlainBody: $"Şifre sıfırlama: {resetUrl}",
            TemplateKey: "auth.password_reset"), ct);

        return Result<Unit>.Ok(Unit.Value);
    }

    public async Task<Result<Unit>> ResetPasswordAsync(ResetPasswordDto dto, CancellationToken ct)
    {
        var validation = await _resetValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid) return FromValidation<Unit>(validation);

        var hash = _tokenHasher.Hash(dto.Token);
        var token = await _db.PasswordResetTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (token is null || !token.IsValid(_clock.UtcNow))
        {
            return Result<Unit>.Fail("auth.invalid_token", ValidationMessages.InvalidOrExpiredToken);
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == token.UserId, ct);
        if (user is null) return Result<Unit>.Fail("auth.invalid_token", ValidationMessages.InvalidOrExpiredToken);

        user.UpdatePasswordHash(_hasher.Hash(dto.Password));
        token.MarkUsed();

        // Tüm aktif refresh tokenları revoke et.
        var activeRefresh = await _db.RefreshTokens.Where(t => t.UserId == user.Id && t.RevokedAtUtc == null).ToListAsync(ct);
        foreach (var rt in activeRefresh) rt.Revoke(null);
        await _db.SaveChangesAsync(ct);
        return Result<Unit>.Ok(Unit.Value);
    }

    public async Task<Result<Unit>> ChangePasswordAsync(Guid userId, ChangePasswordDto dto, CancellationToken ct)
    {
        var validation = await _changeValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid) return FromValidation<Unit>(validation);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return Result<Unit>.Fail("auth.not_found", ValidationMessages.NotAuthenticated);

        if (!_hasher.Verify(dto.CurrentPassword, user.PasswordHash))
        {
            return Result<Unit>.Fail("auth.wrong_current_password", "Mevcut şifre hatalı.",
                new Dictionary<string, string[]> { ["currentPassword"] = ["Mevcut şifre hatalı."] });
        }

        user.UpdatePasswordHash(_hasher.Hash(dto.NewPassword));

        // Şoför ise MustChangePassword flag'ini düşür.
        var driver = await _db.Drivers.FirstOrDefaultAsync(d => d.UserId == userId, ct);
        driver?.ClearMustChangePassword();

        var activeRefresh = await _db.RefreshTokens.Where(t => t.UserId == user.Id && t.RevokedAtUtc == null).ToListAsync(ct);
        foreach (var rt in activeRefresh) rt.Revoke(null);

        await _db.SaveChangesAsync(ct);
        return Result<Unit>.Ok(Unit.Value);
    }

    // ------------- Current user -------------

    public async Task<Result<CurrentUserDto>> GetCurrentUserAsync(Guid userId, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return Result<CurrentUserDto>.Fail("auth.not_found", ValidationMessages.NotAuthenticated);

        return user.Role switch
        {
            UserRole.Customer => await MapCustomer(user, ct),
            UserRole.Driver => await MapDriver(user, ct),
            UserRole.Admin => await MapAdmin(user, ct),
            _ => Result<CurrentUserDto>.Fail("auth.not_found", ValidationMessages.NotAuthenticated),
        };
    }

    public async Task<Result<CurrentUserDto>> UpdateProfileAsync(Guid userId, UpdateProfileDto dto, CancellationToken ct)
    {
        var validation = await _updateProfileValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid) return FromValidation<CurrentUserDto>(validation);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return Result<CurrentUserDto>.Fail("auth.not_found", ValidationMessages.NotAuthenticated);

        switch (user.Role)
        {
            case UserRole.Customer:
                var c = await _db.Customers.FirstOrDefaultAsync(x => x.UserId == userId, ct);
                c?.UpdateProfile(dto.FirstName, dto.LastName, dto.Phone);
                break;
            case UserRole.Driver:
                var d = await _db.Drivers.FirstOrDefaultAsync(x => x.UserId == userId, ct);
                d?.UpdateProfile(dto.FirstName, dto.LastName, dto.Phone);
                break;
            case UserRole.Admin:
                var a = await _db.Admins.FirstOrDefaultAsync(x => x.UserId == userId, ct);
                a?.UpdateFullName($"{dto.FirstName} {dto.LastName}".Trim());
                break;
        }
        await _db.SaveChangesAsync(ct);
        return await GetCurrentUserAsync(userId, ct);
    }

    // ------------- Helpers -------------

    private async Task<AuthTokensDto> IssueTokensAsync(User user, CancellationToken ct)
    {
        var access = _jwt.CreateAccessToken(user.Id, user.Email, user.Role.ToString());
        var (plain, entity) = CreateRefreshToken(user.Id);
        _db.RefreshTokens.Add(entity);
        _ = ct;
        return new AuthTokensDto(access.Token, plain, access.ExpiresAtUtc, entity.ExpiresAtUtc);
    }

    private (string plain, RefreshToken entity) CreateRefreshToken(Guid userId)
    {
        var plain = _jwt.CreateRefreshTokenValue();
        var entity = RefreshToken.Create(
            Guid.NewGuid(),
            userId,
            _tokenHasher.Hash(plain),
            _jwt.GetRefreshTokenExpiresAtUtc(_clock.UtcNow));
        return (plain, entity);
    }

    private async Task<Result<CurrentUserDto>> MapCustomer(User user, CancellationToken ct)
    {
        var c = await _db.Customers.FirstOrDefaultAsync(x => x.UserId == user.Id, ct);
        if (c is null) return Result<CurrentUserDto>.Fail("auth.profile_missing", "Profil bulunamadı.");
        return Result<CurrentUserDto>.Ok(new CurrentUserDto(
            user.Id, user.Email, "Customer", c.FirstName, c.LastName, c.PhoneNumber, false, user.EmailConfirmed));
    }

    private async Task<Result<CurrentUserDto>> MapDriver(User user, CancellationToken ct)
    {
        var d = await _db.Drivers.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.UserId == user.Id, ct);
        if (d is null) return Result<CurrentUserDto>.Fail("auth.profile_missing", "Profil bulunamadı.");
        return Result<CurrentUserDto>.Ok(new CurrentUserDto(
            user.Id, user.Email, "Driver", d.FirstName, d.LastName, d.PhoneNumber, d.MustChangePassword, user.EmailConfirmed));
    }

    private async Task<Result<CurrentUserDto>> MapAdmin(User user, CancellationToken ct)
    {
        var a = await _db.Admins.FirstOrDefaultAsync(x => x.UserId == user.Id, ct);
        if (a is null) return Result<CurrentUserDto>.Fail("auth.profile_missing", "Profil bulunamadı.");
        var parts = a.FullName.Split(' ', 2);
        return Result<CurrentUserDto>.Ok(new CurrentUserDto(
            user.Id, user.Email, "Admin", parts[0], parts.Length > 1 ? parts[1] : string.Empty, string.Empty, false, user.EmailConfirmed));
    }

    private static string HtmlEncode(string s) => System.Net.WebUtility.HtmlEncode(s);

    private static Result<T> FromValidation<T>(FluentValidation.Results.ValidationResult v)
    {
        var dict = v.Errors
            .GroupBy(e => Camel(e.PropertyName))
            .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
        return Result<T>.Fail("validation", "Doğrulama hatası.", dict);
    }

    private static string Camel(string s) => string.IsNullOrEmpty(s) ? s : char.ToLowerInvariant(s[0]) + s[1..];
}

public interface IFrontendUrlProvider
{
    string VerifyEmailUrl(string token);
    string ResetPasswordUrl(string token);
}
