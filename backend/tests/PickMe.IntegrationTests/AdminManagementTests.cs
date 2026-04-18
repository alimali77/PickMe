using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PickMe.Application.Abstractions;
using PickMe.Application.AdminManagement;
using PickMe.Application.Auth;
using PickMe.Application.Reservations;
using PickMe.Domain;
using PickMe.Domain.Entities;
using PickMe.Infrastructure.Persistence;
using PickMe.Infrastructure.Security;
using Xunit;

namespace PickMe.IntegrationTests;

public class AdminManagementTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly TestEmailQueue _emails = new();
    private readonly BcryptPasswordHasher _hasher = new(4);

    private readonly DriverManagementService _driverSvc;
    private readonly RecipientsService _recipientsSvc;
    private readonly FaqManagementService _faqSvc;
    private readonly ContactMessagesService _contactSvc;
    private readonly CustomerAdminService _customerSvc;
    private readonly RatingAdminService _ratingAdminSvc;
    private readonly AdminUsersService _adminSvc;
    private readonly SystemSettingsService _settingsSvc;

    public AdminManagementTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"admin-{Guid.NewGuid()}")
            .Options;
        _db = new ApplicationDbContext(options);

        _driverSvc = new DriverManagementService(_db, _hasher, _emails,
            new CreateDriverValidator(), new UpdateDriverValidator(), NullLogger<DriverManagementService>.Instance);

        _recipientsSvc = new RecipientsService(_db, new CreateRecipientValidator());
        _faqSvc = new FaqManagementService(_db, new CreateFaqValidator(), new UpdateFaqValidator());
        _contactSvc = new ContactMessagesService(_db);
        _customerSvc = new CustomerAdminService(_db);
        _ratingAdminSvc = new RatingAdminService(_db, new FlagRatingValidator());
        _adminSvc = new AdminUsersService(_db, _hasher, new CreateAdminValidator(), new UpdateAdminValidator());
        _settingsSvc = new SystemSettingsService(_db);
    }

    // --------- Drivers ---------

    [Fact]
    public async Task Create_Driver_Inserts_User_And_Driver_And_Sends_Credentials_Mail()
    {
        var result = await _driverSvc.CreateAsync(
            new CreateDriverDto("Mehmet", "Demir", "mehmet@pickme.tr", "05551112233", "Strong1Pass"),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        (await _db.Users.CountAsync()).Should().Be(1);
        (await _db.Drivers.CountAsync()).Should().Be(1);
        _emails.Sent.Should().ContainSingle(m => m.TemplateKey == "driver.account_created");
    }

    [Fact]
    public async Task Create_Driver_Without_Password_Generates_One_And_Sends_Mail()
    {
        var r = await _driverSvc.CreateAsync(
            new CreateDriverDto("Temp", "User", "temp@pickme.tr", "05551112233", null),
            CancellationToken.None);

        r.Success.Should().BeTrue();
        _emails.Sent.Should().ContainSingle(m => m.TemplateKey == "driver.account_created"
            && m.PlainBody.Contains("Şifre:", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Create_Driver_Duplicate_Email_Is_Rejected()
    {
        await _driverSvc.CreateAsync(new CreateDriverDto("Ali", "Bekir", "dup@pickme.tr", "05551112233", "Strong1Pass"), CancellationToken.None);
        var r = await _driverSvc.CreateAsync(new CreateDriverDto("Ceyda", "Demir", "dup@pickme.tr", "05554445566", "Strong1Pass"), CancellationToken.None);
        r.Success.Should().BeFalse();
        r.Code.Should().Be("auth.email_taken");
    }

    [Fact]
    public async Task Deactivate_Driver_With_Active_Assignment_Is_Blocked()
    {
        var create = await _driverSvc.CreateAsync(new CreateDriverDto("Ali", "Bekir", "d1@pickme.tr", "05551112233", "Strong1Pass"), CancellationToken.None);
        var driverId = create.Data;

        // Seed müşteri + atanmış rezervasyon
        var (customer, _) = await CreateCustomerAsync("cust1@pickme.tr");
        var reservation = Reservation.Create(Guid.NewGuid(), customer.Id, ServiceType.Driver, DateTime.UtcNow.AddHours(3), "Somewhere", 41, 29, null);
        reservation.AssignDriver(driverId);
        _db.Reservations.Add(reservation);
        await _db.SaveChangesAsync();

        var result = await _driverSvc.SetActiveAsync(driverId, active: false, CancellationToken.None);
        result.Success.Should().BeFalse();
        result.Code.Should().Be("driver.has_active_assignments");
    }

    [Fact]
    public async Task Soft_Delete_Driver_With_Active_Assignment_Is_Blocked()
    {
        var create = await _driverSvc.CreateAsync(new CreateDriverDto("Ali", "Bekir", "d2@pickme.tr", "05551112233", "Strong1Pass"), CancellationToken.None);
        var driverId = create.Data;
        var (customer, _) = await CreateCustomerAsync("cust2@pickme.tr");
        var reservation = Reservation.Create(Guid.NewGuid(), customer.Id, ServiceType.Driver, DateTime.UtcNow.AddHours(3), "Somewhere", 41, 29, null);
        reservation.AssignDriver(driverId);
        _db.Reservations.Add(reservation);
        await _db.SaveChangesAsync();

        var result = await _driverSvc.SoftDeleteAsync(driverId, CancellationToken.None);
        result.Success.Should().BeFalse();
        result.Code.Should().Be("driver.has_active_assignments");
    }

    [Fact]
    public async Task Reset_Driver_Password_Sends_Mail_And_Forces_Change()
    {
        var create = await _driverSvc.CreateAsync(new CreateDriverDto("Ali", "Bekir", "reset@pickme.tr", "05551112233", "Strong1Pass"), CancellationToken.None);
        // İlk şifre değişikliği zaten gerekliydi — reset sonrası hâlâ true kalmalı
        var driver = await _db.Drivers.FirstAsync();
        driver.ClearMustChangePassword();
        await _db.SaveChangesAsync();

        _emails.Sent.Clear();
        var result = await _driverSvc.ResetPasswordAsync(create.Data, CancellationToken.None);
        result.Success.Should().BeTrue();

        var fresh = await _db.Drivers.FirstAsync(d => d.Id == create.Data);
        fresh.MustChangePassword.Should().BeTrue();
        _emails.Sent.Should().ContainSingle(m => m.TemplateKey == "driver.password_reset_by_admin");
    }

    // --------- Recipients ---------

    [Fact]
    public async Task Cannot_Deactivate_Last_Active_Recipient()
    {
        var add = await _recipientsSvc.AddAsync(new CreateRecipientDto("only@pickme.tr"), CancellationToken.None);
        add.Success.Should().BeTrue();

        var result = await _recipientsSvc.SetActiveAsync(add.Data, active: false, CancellationToken.None);
        result.Success.Should().BeFalse();
        result.Code.Should().Be("recipient.last_active");
    }

    [Fact]
    public async Task Cannot_Delete_Last_Active_Recipient()
    {
        var add = await _recipientsSvc.AddAsync(new CreateRecipientDto("only@pickme.tr"), CancellationToken.None);
        var result = await _recipientsSvc.DeleteAsync(add.Data, CancellationToken.None);
        result.Success.Should().BeFalse();
        result.Code.Should().Be("recipient.last_active");
    }

    [Fact]
    public async Task Can_Delete_Inactive_Recipient()
    {
        var a = await _recipientsSvc.AddAsync(new CreateRecipientDto("a@pickme.tr"), CancellationToken.None);
        var b = await _recipientsSvc.AddAsync(new CreateRecipientDto("b@pickme.tr"), CancellationToken.None);
        await _recipientsSvc.SetActiveAsync(a.Data, false, CancellationToken.None);

        var result = await _recipientsSvc.DeleteAsync(a.Data, CancellationToken.None);
        result.Success.Should().BeTrue();
        (await _db.AdminNotificationRecipients.CountAsync()).Should().Be(1);
        _ = b;
    }

    [Fact]
    public async Task Duplicate_Recipient_Email_Is_Rejected()
    {
        await _recipientsSvc.AddAsync(new CreateRecipientDto("x@pickme.tr"), CancellationToken.None);
        var dup = await _recipientsSvc.AddAsync(new CreateRecipientDto("X@PickMe.tr"), CancellationToken.None);
        dup.Success.Should().BeFalse();
        dup.Code.Should().Be("recipient.duplicate");
    }

    // --------- FAQ ---------

    [Fact]
    public async Task Faq_CRUD_Roundtrip()
    {
        var create = await _faqSvc.CreateAsync(new CreateFaqDto("Q1?", "A1.", 1), CancellationToken.None);
        create.Success.Should().BeTrue();

        var list = await _faqSvc.ListAsync(CancellationToken.None);
        list.Data!.Should().HaveCount(1);

        var update = await _faqSvc.UpdateAsync(create.Data, new UpdateFaqDto("Q1 güncel?", "A1 güncel.", 2, false), CancellationToken.None);
        update.Success.Should().BeTrue();

        var del = await _faqSvc.DeleteAsync(create.Data, CancellationToken.None);
        del.Success.Should().BeTrue();
        (await _db.Faqs.CountAsync()).Should().Be(0);
    }

    // --------- Admin users ---------

    [Fact]
    public async Task Cannot_Delete_Self()
    {
        var u = User.Create(Guid.NewGuid(), "first@pickme.tr", _hasher.Hash("x"), UserRole.Admin);
        u.ConfirmEmail();
        var admin = Admin.Create(Guid.NewGuid(), u.Id, "First Admin");
        _db.Users.Add(u);
        _db.Admins.Add(admin);
        await _db.SaveChangesAsync();

        var result = await _adminSvc.DeleteAsync(admin.Id, currentAdminUserId: u.Id, CancellationToken.None);
        result.Success.Should().BeFalse();
        result.Code.Should().Be("admin.cannot_delete_self");
    }

    [Fact]
    public async Task Cannot_Delete_Last_Admin()
    {
        var u = User.Create(Guid.NewGuid(), "first@pickme.tr", _hasher.Hash("x"), UserRole.Admin);
        u.ConfirmEmail();
        var admin = Admin.Create(Guid.NewGuid(), u.Id, "First");
        _db.Users.Add(u);
        _db.Admins.Add(admin);
        await _db.SaveChangesAsync();

        // Başka bir admin user ID'sinden silmeyi dene — yine de son admin olduğu için reddedilmeli
        var result = await _adminSvc.DeleteAsync(admin.Id, currentAdminUserId: Guid.NewGuid(), CancellationToken.None);
        result.Success.Should().BeFalse();
        result.Code.Should().Be("admin.last_admin");
    }

    [Fact]
    public async Task Can_Delete_Admin_If_More_Than_One_And_Not_Self()
    {
        var u1 = User.Create(Guid.NewGuid(), "first@pickme.tr", _hasher.Hash("x"), UserRole.Admin); u1.ConfirmEmail();
        var a1 = Admin.Create(Guid.NewGuid(), u1.Id, "First");
        var u2 = User.Create(Guid.NewGuid(), "second@pickme.tr", _hasher.Hash("x"), UserRole.Admin); u2.ConfirmEmail();
        var a2 = Admin.Create(Guid.NewGuid(), u2.Id, "Second");
        _db.Users.AddRange(u1, u2);
        _db.Admins.AddRange(a1, a2);
        await _db.SaveChangesAsync();

        var result = await _adminSvc.DeleteAsync(a2.Id, currentAdminUserId: u1.Id, CancellationToken.None);
        result.Success.Should().BeTrue();
        (await _db.Admins.CountAsync()).Should().Be(1);
        (await _db.Users.CountAsync(u => u.Role == UserRole.Admin)).Should().Be(1);
    }

    // --------- Ratings admin (flag → driver avg update) ---------

    [Fact]
    public async Task Flag_Rating_Recomputes_Driver_Average()
    {
        var (customer, _) = await CreateCustomerAsync("flag@pickme.tr");
        var driverCreate = await _driverSvc.CreateAsync(new CreateDriverDto("Deniz", "Var", "dv@pickme.tr", "05551112233", "Strong1Pass"), CancellationToken.None);
        var driverId = driverCreate.Data;
        var reservation = Reservation.Create(Guid.NewGuid(), customer.Id, ServiceType.Driver, DateTime.UtcNow.AddHours(3), "a", 41, 29, null);
        reservation.AssignDriver(driverId);
        reservation.StartTrip();
        reservation.CompleteTrip();
        _db.Reservations.Add(reservation);

        var ratingHigh = Rating.Create(Guid.NewGuid(), reservation.Id, customer.Id, driverId, 5, "great");

        var r2 = Reservation.Create(Guid.NewGuid(), customer.Id, ServiceType.Driver, DateTime.UtcNow.AddHours(3), "b", 41, 29, null);
        r2.AssignDriver(driverId); r2.StartTrip(); r2.CompleteTrip();
        _db.Reservations.Add(r2);
        var ratingLow = Rating.Create(Guid.NewGuid(), r2.Id, customer.Id, driverId, 1, "bad");
        _db.Ratings.AddRange(ratingHigh, ratingLow);
        await _db.SaveChangesAsync();

        // İlk avg = 3
        var driverBefore = await _db.Drivers.FirstAsync(d => d.Id == driverId);
        driverBefore.RecalculateRating(3m, 2); await _db.SaveChangesAsync();

        // 1 puanlı yorum flag'lenince ortalama 5'e çıkmalı
        var flag = await _ratingAdminSvc.FlagAsync(ratingLow.Id, new FlagRatingDto("uygunsuz"), CancellationToken.None);
        flag.Success.Should().BeTrue();

        var driverAfter = await _db.Drivers.FirstAsync(d => d.Id == driverId);
        driverAfter.AverageRating.Should().Be(5m);
        driverAfter.TotalTrips.Should().Be(1);
    }

    // --------- Customers ---------

    [Fact]
    public async Task Customer_List_Reflects_Registered_Customers_With_Reservation_Count()
    {
        var (c1, _) = await CreateCustomerAsync("c1@pickme.tr");
        var (_, _) = await CreateCustomerAsync("c2@pickme.tr");

        _db.Reservations.Add(Reservation.Create(Guid.NewGuid(), c1.Id, ServiceType.Driver, DateTime.UtcNow.AddHours(3), "x", 41, 29, null));
        _db.Reservations.Add(Reservation.Create(Guid.NewGuid(), c1.Id, ServiceType.Valet, DateTime.UtcNow.AddHours(4), "y", 41, 29, null));
        await _db.SaveChangesAsync();

        var list = await _customerSvc.ListAsync(null, 1, 10, CancellationToken.None);
        list.Data!.Items.Should().HaveCount(2);
        list.Data.Items.First(i => i.Email == "c1@pickme.tr").ReservationCount.Should().Be(2);
        list.Data.Items.First(i => i.Email == "c2@pickme.tr").ReservationCount.Should().Be(0);
    }

    // --------- System settings ---------

    [Fact]
    public async Task Settings_Upsert_And_Mask_Sensitive_Values()
    {
        await _settingsSvc.UpdateAsync(new UpdateSystemSettingsDto(new Dictionary<string, string>
        {
            ["whatsapp.number"] = "905551234567",
            ["google.maps.api_key"] = "verysecretkey1234567890",
        }), CancellationToken.None);

        var list = await _settingsSvc.ListAsync(CancellationToken.None);
        list.Data!.Should().HaveCount(2);
        list.Data.First(s => s.Key == "whatsapp.number").Value.Should().Be("905551234567");
        list.Data.First(s => s.Key == "google.maps.api_key").Value.Should().NotBe("verysecretkey1234567890");
        list.Data.First(s => s.Key == "google.maps.api_key").IsSensitive.Should().BeTrue();
    }

    [Fact]
    public async Task Public_Setting_Only_Returns_Allowed_Keys()
    {
        await _settingsSvc.UpdateAsync(new UpdateSystemSettingsDto(new Dictionary<string, string>
        {
            ["whatsapp.number"] = "905551234567",
            ["google.maps.api_key"] = "secret",
        }), CancellationToken.None);

        var ok = await _settingsSvc.GetPublicAsync("whatsapp.number", CancellationToken.None);
        ok.Success.Should().BeTrue();
        ok.Data.Should().Be("905551234567");

        var denied = await _settingsSvc.GetPublicAsync("google.maps.api_key", CancellationToken.None);
        denied.Success.Should().BeFalse();
        denied.Code.Should().Be("settings.not_public");
    }

    // --------- Helpers ---------

    private async Task<(Customer customer, User user)> CreateCustomerAsync(string email)
    {
        var uid = Guid.NewGuid();
        var user = User.Create(uid, email, _hasher.Hash("Strong1Pass"), UserRole.Customer);
        user.ConfirmEmail();
        var cust = Customer.Create(Guid.NewGuid(), uid, "Cust", "Test", "05559990000", true);
        _db.Users.Add(user);
        _db.Customers.Add(cust);
        await _db.SaveChangesAsync();
        return (cust, user);
    }

    public void Dispose() => _db.Dispose();

    private sealed class TestEmailQueue : IEmailQueue
    {
        public List<EmailMessage> Sent { get; } = [];
        public Task EnqueueAsync(EmailMessage m, CancellationToken ct = default) { Sent.Add(m); return Task.CompletedTask; }
    }
}
