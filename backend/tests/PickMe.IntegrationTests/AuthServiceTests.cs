using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PickMe.Application.Abstractions;
using PickMe.Application.Auth;
using PickMe.Domain;
using PickMe.Domain.Entities;
using PickMe.Infrastructure.Email;
using PickMe.Infrastructure.Persistence;
using PickMe.Infrastructure.Security;
using Xunit;

namespace PickMe.IntegrationTests;

public class AuthServiceTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly AuthService _svc;
    private readonly TestEmailQueue _emailQueue = new();
    private readonly TestClock _clock = new();
    private readonly TestUrls _urls = new();

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"auth-{Guid.NewGuid()}")
            .Options;
        _db = new ApplicationDbContext(options);

        var jwt = new JwtTokenService(Options.Create(new JwtOptions
        {
            Secret = "integration-test-secret-that-is-at-least-32-bytes-long-ok",
            Issuer = "test",
            Audience = "test",
            AccessTtlMinutes = 60,
            RefreshTtlDays = 7,
        }));

        _svc = new AuthService(
            _db,
            new BcryptPasswordHasher(4),
            new Sha256TokenHasher(),
            new OpaqueTokenGenerator(),
            jwt,
            _emailQueue,
            _clock,
            new RegisterCustomerValidator(),
            new LoginValidator(),
            new ForgotPasswordValidator(),
            new ResetPasswordValidator(),
            new ChangePasswordValidator(),
            new UpdateProfileValidator(),
            _urls,
            NullLogger<AuthService>.Instance);
    }

    [Fact]
    public async Task Register_Creates_User_Customer_And_Sends_Verification_Email()
    {
        var dto = ValidRegister();
        var result = await _svc.RegisterCustomerAsync(dto, CancellationToken.None);
        result.Success.Should().BeTrue();

        (await _db.Users.CountAsync()).Should().Be(1);
        (await _db.Customers.CountAsync()).Should().Be(1);
        (await _db.EmailVerificationTokens.CountAsync()).Should().Be(1);
        _emailQueue.Sent.Should().ContainSingle(m => m.TemplateKey == "auth.verify_email");
    }

    [Fact]
    public async Task Register_Duplicate_Email_Returns_Conflict()
    {
        await _svc.RegisterCustomerAsync(ValidRegister(), CancellationToken.None);
        var second = await _svc.RegisterCustomerAsync(ValidRegister(), CancellationToken.None);
        second.Success.Should().BeFalse();
        second.Code.Should().Be("auth.email_taken");
    }

    [Fact]
    public async Task Cannot_Login_Before_Email_Verified()
    {
        await _svc.RegisterCustomerAsync(ValidRegister(), CancellationToken.None);
        var login = await _svc.LoginAsync(new LoginDto("ali@pickme.tr", "Strong1Pass"), CancellationToken.None);
        login.Success.Should().BeFalse();
        login.Code.Should().Be("auth.email_not_verified");
    }

    [Fact]
    public async Task Full_Register_Verify_Login_Refresh_Logout_Flow()
    {
        await _svc.RegisterCustomerAsync(ValidRegister(), CancellationToken.None);
        var tokenEntity = await _db.EmailVerificationTokens.FirstAsync();
        // plain token ver e-postaya giden linkten alınır — burada email queue'dan parse edelim
        var verifyMsg = _emailQueue.Sent.Single(m => m.TemplateKey == "auth.verify_email");
        var plainToken = ExtractTokenFromUrl(verifyMsg.PlainBody);

        var verify = await _svc.VerifyEmailAsync(new VerifyEmailDto(plainToken), CancellationToken.None);
        verify.Success.Should().BeTrue();

        var login = await _svc.LoginAsync(new LoginDto("ali@pickme.tr", "Strong1Pass"), CancellationToken.None);
        login.Success.Should().BeTrue();
        login.Data!.AccessToken.Should().NotBeNullOrWhiteSpace();
        login.Data.RefreshToken.Should().NotBeNullOrWhiteSpace();

        var refresh = await _svc.RefreshAsync(new RefreshDto(login.Data.RefreshToken), CancellationToken.None);
        refresh.Success.Should().BeTrue();
        refresh.Data!.RefreshToken.Should().NotBe(login.Data.RefreshToken);

        // Rotation: eski refresh artık çalışmamalı
        var reused = await _svc.RefreshAsync(new RefreshDto(login.Data.RefreshToken), CancellationToken.None);
        reused.Success.Should().BeFalse();

        var logout = await _svc.LogoutAsync(new LogoutDto(refresh.Data.RefreshToken), CancellationToken.None);
        logout.Success.Should().BeTrue();

        var afterLogout = await _svc.RefreshAsync(new RefreshDto(refresh.Data.RefreshToken), CancellationToken.None);
        afterLogout.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Five_Failed_Logins_Lock_Account()
    {
        await _svc.RegisterCustomerAsync(ValidRegister(), CancellationToken.None);
        var verifyMsg = _emailQueue.Sent.Single(m => m.TemplateKey == "auth.verify_email");
        await _svc.VerifyEmailAsync(new VerifyEmailDto(ExtractTokenFromUrl(verifyMsg.PlainBody)), CancellationToken.None);

        for (int i = 0; i < 5; i++)
        {
            var r = await _svc.LoginAsync(new LoginDto("ali@pickme.tr", "WRONG_PASS_1"), CancellationToken.None);
            r.Success.Should().BeFalse();
        }
        var locked = await _svc.LoginAsync(new LoginDto("ali@pickme.tr", "Strong1Pass"), CancellationToken.None);
        locked.Success.Should().BeFalse();
        locked.Code.Should().Be("auth.locked");
    }

    [Fact]
    public async Task Forgot_Password_With_Unknown_Email_Returns_Ok_Without_Sending_Mail()
    {
        var r = await _svc.ForgotPasswordAsync(new ForgotPasswordDto("nonexistent@pickme.tr"), CancellationToken.None);
        r.Success.Should().BeTrue();
        _emailQueue.Sent.Should().BeEmpty();
    }

    [Fact]
    public async Task Forgot_And_Reset_Password_Flow()
    {
        await _svc.RegisterCustomerAsync(ValidRegister(), CancellationToken.None);
        var verifyMsg = _emailQueue.Sent.Single(m => m.TemplateKey == "auth.verify_email");
        await _svc.VerifyEmailAsync(new VerifyEmailDto(ExtractTokenFromUrl(verifyMsg.PlainBody)), CancellationToken.None);

        _emailQueue.Sent.Clear();
        await _svc.ForgotPasswordAsync(new ForgotPasswordDto("ali@pickme.tr"), CancellationToken.None);
        var resetMsg = _emailQueue.Sent.Single(m => m.TemplateKey == "auth.password_reset");
        var plainToken = ExtractTokenFromUrl(resetMsg.PlainBody);

        var reset = await _svc.ResetPasswordAsync(new ResetPasswordDto(plainToken, "NewStrong1Pass", "NewStrong1Pass"), CancellationToken.None);
        reset.Success.Should().BeTrue();

        // Yeni şifreyle giriş çalışmalı
        var loginNew = await _svc.LoginAsync(new LoginDto("ali@pickme.tr", "NewStrong1Pass"), CancellationToken.None);
        loginNew.Success.Should().BeTrue();

        // Eski şifreyle giriş başarısız
        var loginOld = await _svc.LoginAsync(new LoginDto("ali@pickme.tr", "Strong1Pass"), CancellationToken.None);
        loginOld.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Invalid_Verification_Token_Is_Rejected()
    {
        var r = await _svc.VerifyEmailAsync(new VerifyEmailDto("not-a-real-token"), CancellationToken.None);
        r.Success.Should().BeFalse();
        r.Code.Should().Be("auth.invalid_token");
    }

    [Fact]
    public async Task Change_Password_Revokes_All_Refresh_Tokens()
    {
        await _svc.RegisterCustomerAsync(ValidRegister(), CancellationToken.None);
        var verifyMsg = _emailQueue.Sent.Single(m => m.TemplateKey == "auth.verify_email");
        await _svc.VerifyEmailAsync(new VerifyEmailDto(ExtractTokenFromUrl(verifyMsg.PlainBody)), CancellationToken.None);
        var login = await _svc.LoginAsync(new LoginDto("ali@pickme.tr", "Strong1Pass"), CancellationToken.None);
        login.Success.Should().BeTrue();

        var userId = (await _db.Users.FirstAsync()).Id;
        var change = await _svc.ChangePasswordAsync(userId, new ChangePasswordDto("Strong1Pass", "NewStrong1Pass", "NewStrong1Pass"), CancellationToken.None);
        change.Success.Should().BeTrue();

        // Eski refresh artık çalışmamalı
        var refresh = await _svc.RefreshAsync(new RefreshDto(login.Data!.RefreshToken), CancellationToken.None);
        refresh.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Change_Password_With_Wrong_Current_Fails()
    {
        await _svc.RegisterCustomerAsync(ValidRegister(), CancellationToken.None);
        var userId = (await _db.Users.FirstAsync()).Id;
        var r = await _svc.ChangePasswordAsync(userId, new ChangePasswordDto("wrong_current", "NewStrong1Pass", "NewStrong1Pass"), CancellationToken.None);
        r.Success.Should().BeFalse();
        r.Code.Should().Be("auth.wrong_current_password");
    }

    [Fact]
    public async Task GetCurrentUser_Returns_Profile_For_Customer()
    {
        await _svc.RegisterCustomerAsync(ValidRegister(), CancellationToken.None);
        var userId = (await _db.Users.FirstAsync()).Id;
        var r = await _svc.GetCurrentUserAsync(userId, CancellationToken.None);
        r.Success.Should().BeTrue();
        r.Data!.Email.Should().Be("ali@pickme.tr");
        r.Data.Role.Should().Be("Customer");
        r.Data.FirstName.Should().Be("Ali");
        r.Data.EmailConfirmed.Should().BeFalse();
    }

    [Fact]
    public async Task Invalid_Email_Format_Fails_Validation_With_Shared_Message()
    {
        var r = await _svc.RegisterCustomerAsync(ValidRegister() with { Email = "not-email" }, CancellationToken.None);
        r.Success.Should().BeFalse();
        r.Errors.Should().NotBeNull();
        r.Errors!["email"].Should().Contain(ValidationMessages.EmailFormat);
    }

    private static RegisterCustomerDto ValidRegister() => new(
        "Ali", "Yılmaz", "ali@pickme.tr", "05551234567",
        "Strong1Pass", "Strong1Pass", true);

    private static string ExtractTokenFromUrl(string body)
    {
        var idx = body.IndexOf("token=", StringComparison.Ordinal);
        if (idx < 0) return string.Empty;
        var tail = body[(idx + "token=".Length)..];
        var end = tail.IndexOfAny([' ', '\n', '\r', '"', '>', '<']);
        var raw = end < 0 ? tail : tail[..end];
        return Uri.UnescapeDataString(raw.Trim());
    }

    public void Dispose() => _db.Dispose();

    private sealed class TestEmailQueue : IEmailQueue
    {
        public List<EmailMessage> Sent { get; } = [];
        public Task EnqueueAsync(EmailMessage message, CancellationToken ct = default)
        {
            Sent.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class TestClock : IClock { public DateTime UtcNow { get; set; } = DateTime.UtcNow; }

    private sealed class TestUrls : IFrontendUrlProvider
    {
        public string VerifyEmailUrl(string token) => $"https://test/eposta-dogrula?token={Uri.EscapeDataString(token)}";
        public string ResetPasswordUrl(string token) => $"https://test/sifre-sifirla?token={Uri.EscapeDataString(token)}";
    }
}
