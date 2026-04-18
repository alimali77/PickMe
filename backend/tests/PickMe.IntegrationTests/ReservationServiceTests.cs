using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PickMe.Application.Abstractions;
using PickMe.Application.Auth;
using PickMe.Application.Reservations;
using PickMe.Domain;
using PickMe.Domain.Entities;
using PickMe.Infrastructure.Persistence;
using Xunit;

namespace PickMe.IntegrationTests;

public class ReservationServiceTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly ReservationService _svc;
    private readonly RatingService _rating;
    private readonly TestEmailQueue _emails = new();
    private readonly TestClock _clock = new();

    // Seed dataset IDs
    private readonly Guid _customerUserId;
    private readonly Guid _customerId;
    private readonly Guid _otherCustomerUserId;
    private readonly Guid _driverUserId;
    private readonly Guid _driverId;
    private readonly Guid _adminRecipientId;

    public ReservationServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"res-{Guid.NewGuid()}")
            .Options;
        _db = new ApplicationDbContext(options);

        _svc = new ReservationService(
            _db, _emails, _clock,
            new CreateReservationValidator(),
            new AssignDriverValidator(),
            new AdminCancelReservationValidator(),
            NullLogger<ReservationService>.Instance);

        _rating = new RatingService(_db, _clock, new RateReservationValidator(), NullLogger<RatingService>.Instance);

        // ---- Seed ----
        _customerUserId = Guid.NewGuid();
        var customerUser = User.Create(_customerUserId, "ali@pickme.tr", "hash", UserRole.Customer);
        customerUser.ConfirmEmail();
        _customerId = Guid.NewGuid();
        var customer = Customer.Create(_customerId, _customerUserId, "Ali", "Yılmaz", "05551112233", true);

        _otherCustomerUserId = Guid.NewGuid();
        var otherUser = User.Create(_otherCustomerUserId, "veli@pickme.tr", "hash", UserRole.Customer);
        otherUser.ConfirmEmail();
        var otherCustomer = Customer.Create(Guid.NewGuid(), _otherCustomerUserId, "Veli", "Kaya", "05554445566", true);

        _driverUserId = Guid.NewGuid();
        var driverUser = User.Create(_driverUserId, "mehmet@pickme.tr", "hash", UserRole.Driver);
        driverUser.ConfirmEmail();
        _driverId = Guid.NewGuid();
        var driver = Driver.Create(_driverId, _driverUserId, "Mehmet", "Demir", "05557778899");

        _adminRecipientId = Guid.NewGuid();
        var recipient = AdminNotificationRecipient.Create(_adminRecipientId, "admin@pickme.tr");

        _db.Users.AddRange(customerUser, otherUser, driverUser);
        _db.Customers.AddRange(customer, otherCustomer);
        _db.Drivers.Add(driver);
        _db.AdminNotificationRecipients.Add(recipient);
        _db.SaveChanges();
    }

    // ----------- Create -----------

    [Fact]
    public async Task Customer_Can_Create_Reservation_And_Admin_Mail_Is_Sent()
    {
        var result = await _svc.CreateAsync(_customerUserId, ValidCreate(), CancellationToken.None);
        result.Success.Should().BeTrue();

        var list = await _db.Reservations.ToListAsync();
        list.Should().ContainSingle(r => r.CustomerId == _customerId && r.Status == ReservationStatus.Pending);
        _emails.Sent.Should().ContainSingle(m => m.TemplateKey == "reservation.new_admin" && m.To == "admin@pickme.tr");
    }

    [Fact]
    public async Task Create_Validation_ReservationTooSoon_Fails()
    {
        var bad = ValidCreate() with { ReservationDateTimeUtc = DateTime.UtcNow.AddMinutes(10) };
        var result = await _svc.CreateAsync(_customerUserId, bad, CancellationToken.None);
        result.Success.Should().BeFalse();
        result.Code.Should().Be("validation");
    }

    [Fact]
    public async Task Create_Validation_LatLng_OutsideTurkey_Fails()
    {
        var bad = ValidCreate() with { Lat = 0, Lng = 0 };
        var result = await _svc.CreateAsync(_customerUserId, bad, CancellationToken.None);
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Create_Validation_PlaceNotSelectedFromAutocomplete_Fails()
    {
        var bad = ValidCreate() with { PlaceSelectedFromAutocomplete = false };
        var result = await _svc.CreateAsync(_customerUserId, bad, CancellationToken.None);
        result.Success.Should().BeFalse();
        result.Errors.Should().ContainKey("placeSelectedFromAutocomplete");
    }

    [Fact]
    public async Task Create_Fails_When_No_Active_Admin_Recipient()
    {
        var recipient = await _db.AdminNotificationRecipients.FirstAsync();
        recipient.SetActive(false);
        await _db.SaveChangesAsync();

        var result = await _svc.CreateAsync(_customerUserId, ValidCreate(), CancellationToken.None);
        result.Success.Should().BeFalse();
        result.Code.Should().Be("reservation.no_recipients");
    }

    // ----------- Access control -----------

    [Fact]
    public async Task Customer_Cannot_See_Another_Customers_Reservation()
    {
        var created = await _svc.CreateAsync(_customerUserId, ValidCreate(), CancellationToken.None);
        var reservationId = created.Data;

        var other = await _svc.GetOwnDetailAsync(_otherCustomerUserId, reservationId, CancellationToken.None);
        other.Success.Should().BeFalse();
        other.Code.Should().Be("reservation.not_found"); // 404 — IDOR engeli
    }

    [Fact]
    public async Task Driver_Cannot_See_Task_Not_Assigned_To_Them()
    {
        var created = await _svc.CreateAsync(_customerUserId, ValidCreate(), CancellationToken.None);
        var reservationId = created.Data;

        var driverView = await _svc.DriverGetOwnTaskAsync(_driverUserId, reservationId, CancellationToken.None);
        driverView.Success.Should().BeFalse();
        driverView.Code.Should().Be("reservation.not_found");
    }

    // ----------- State machine through full happy path -----------

    [Fact]
    public async Task Full_Happy_Path_Create_Assign_Start_Complete()
    {
        var createResult = await _svc.CreateAsync(_customerUserId, ValidCreate(), CancellationToken.None);
        var reservationId = createResult.Data;

        _emails.Sent.Clear();

        var assign = await _svc.AdminAssignAsync(reservationId, new AssignDriverDto(_driverId), CancellationToken.None);
        assign.Success.Should().BeTrue();
        _emails.Sent.Should().Contain(m => m.TemplateKey == "reservation.assigned_customer");
        _emails.Sent.Should().Contain(m => m.TemplateKey == "reservation.assigned_driver");

        var start = await _svc.DriverStartAsync(_driverUserId, reservationId, CancellationToken.None);
        start.Success.Should().BeTrue();

        _emails.Sent.Clear();
        var complete = await _svc.DriverCompleteAsync(_driverUserId, reservationId, CancellationToken.None);
        complete.Success.Should().BeTrue();
        _emails.Sent.Should().ContainSingle(m => m.TemplateKey == "reservation.completed_rating_invite");

        var final = await _db.Reservations.FirstAsync(r => r.Id == reservationId);
        final.Status.Should().Be(ReservationStatus.Completed);
        final.StartedAtUtc.Should().NotBeNull();
        final.CompletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task Customer_Cancel_Allowed_In_Pending_But_Not_Assigned()
    {
        var createResult = await _svc.CreateAsync(_customerUserId, ValidCreate(), CancellationToken.None);
        var reservationId = createResult.Data;

        // Pending — OK
        var cancelPending = await _svc.CancelByCustomerAsync(_customerUserId, reservationId, CancellationToken.None);
        cancelPending.Success.Should().BeTrue();

        // Yeni rezervasyon, Assigned durumunda — customer cancel reddedilmeli
        var r2 = await _svc.CreateAsync(_customerUserId, ValidCreate(), CancellationToken.None);
        await _svc.AdminAssignAsync(r2.Data, new AssignDriverDto(_driverId), CancellationToken.None);
        var cancelAssigned = await _svc.CancelByCustomerAsync(_customerUserId, r2.Data, CancellationToken.None);
        cancelAssigned.Success.Should().BeFalse();
        cancelAssigned.Code.Should().Be("reservation.invalid_transition");
    }

    [Fact]
    public async Task Admin_Cannot_Cancel_Completed_Reservation()
    {
        var createResult = await _svc.CreateAsync(_customerUserId, ValidCreate(), CancellationToken.None);
        await _svc.AdminAssignAsync(createResult.Data, new AssignDriverDto(_driverId), CancellationToken.None);
        await _svc.DriverStartAsync(_driverUserId, createResult.Data, CancellationToken.None);
        await _svc.DriverCompleteAsync(_driverUserId, createResult.Data, CancellationToken.None);

        var cancel = await _svc.AdminCancelAsync(createResult.Data, new CancelReservationDto("too late"), CancellationToken.None);
        cancel.Success.Should().BeFalse();
        cancel.Code.Should().Be("reservation.invalid_transition");
    }

    [Fact]
    public async Task Admin_Cancel_Without_Reason_Is_Rejected_By_Validator()
    {
        var createResult = await _svc.CreateAsync(_customerUserId, ValidCreate(), CancellationToken.None);
        var cancel = await _svc.AdminCancelAsync(createResult.Data, new CancelReservationDto(null), CancellationToken.None);
        cancel.Success.Should().BeFalse();
        cancel.Code.Should().Be("validation");
    }

    [Fact]
    public async Task Admin_Can_Reassign_Driver_While_Assigned()
    {
        var r = await _svc.CreateAsync(_customerUserId, ValidCreate(), CancellationToken.None);
        await _svc.AdminAssignAsync(r.Data, new AssignDriverDto(_driverId), CancellationToken.None);

        // Aynı endpoint ile başka şoför: reassign olarak kabul edilmeli
        var secondDriverUserId = Guid.NewGuid();
        var secondDriverId = Guid.NewGuid();
        var u2 = User.Create(secondDriverUserId, "ikinci@pickme.tr", "h", UserRole.Driver);
        u2.ConfirmEmail();
        _db.Users.Add(u2);
        _db.Drivers.Add(Driver.Create(secondDriverId, secondDriverUserId, "İkinci", "Sürücü", "05550001122"));
        await _db.SaveChangesAsync();

        var reassign = await _svc.AdminAssignAsync(r.Data, new AssignDriverDto(secondDriverId), CancellationToken.None);
        reassign.Success.Should().BeTrue();
        var final = await _db.Reservations.FirstAsync(x => x.Id == r.Data);
        final.DriverId.Should().Be(secondDriverId);
    }

    [Fact]
    public async Task Driver_Cannot_Start_Others_Reservation()
    {
        var r = await _svc.CreateAsync(_customerUserId, ValidCreate(), CancellationToken.None);
        await _svc.AdminAssignAsync(r.Data, new AssignDriverDto(_driverId), CancellationToken.None);

        var attackerUser = Guid.NewGuid();
        var attackerUserEntity = User.Create(attackerUser, "attacker@pickme.tr", "h", UserRole.Driver);
        attackerUserEntity.ConfirmEmail();
        _db.Users.Add(attackerUserEntity);
        _db.Drivers.Add(Driver.Create(Guid.NewGuid(), attackerUser, "Saldırgan", "User", "05551112233"));
        await _db.SaveChangesAsync();

        var start = await _svc.DriverStartAsync(attackerUser, r.Data, CancellationToken.None);
        start.Success.Should().BeFalse();
        start.Code.Should().Be("reservation.not_found");
    }

    // ----------- Rating -----------

    [Fact]
    public async Task Rating_Updates_Driver_Average_And_Total_Trips()
    {
        var r = await _svc.CreateAsync(_customerUserId, ValidCreate(), CancellationToken.None);
        await _svc.AdminAssignAsync(r.Data, new AssignDriverDto(_driverId), CancellationToken.None);
        await _svc.DriverStartAsync(_driverUserId, r.Data, CancellationToken.None);
        await _svc.DriverCompleteAsync(_driverUserId, r.Data, CancellationToken.None);

        var rating = await _rating.RateAsync(_customerUserId, r.Data, new RateReservationDto(5, "Harika"), CancellationToken.None);
        rating.Success.Should().BeTrue();

        var driver = await _db.Drivers.FirstAsync(d => d.Id == _driverId);
        driver.AverageRating.Should().Be(5m);
        driver.TotalTrips.Should().Be(1);
    }

    [Fact]
    public async Task Cannot_Rate_Same_Reservation_Twice()
    {
        var r = await _svc.CreateAsync(_customerUserId, ValidCreate(), CancellationToken.None);
        await _svc.AdminAssignAsync(r.Data, new AssignDriverDto(_driverId), CancellationToken.None);
        await _svc.DriverStartAsync(_driverUserId, r.Data, CancellationToken.None);
        await _svc.DriverCompleteAsync(_driverUserId, r.Data, CancellationToken.None);

        var first = await _rating.RateAsync(_customerUserId, r.Data, new RateReservationDto(4, null), CancellationToken.None);
        first.Success.Should().BeTrue();

        var second = await _rating.RateAsync(_customerUserId, r.Data, new RateReservationDto(5, null), CancellationToken.None);
        second.Success.Should().BeFalse();
        second.Code.Should().Be("rating.already_given");
    }

    [Fact]
    public async Task Cannot_Rate_Reservation_Not_Completed()
    {
        var r = await _svc.CreateAsync(_customerUserId, ValidCreate(), CancellationToken.None);
        var rating = await _rating.RateAsync(_customerUserId, r.Data, new RateReservationDto(5, null), CancellationToken.None);
        rating.Success.Should().BeFalse();
        rating.Code.Should().Be("rating.not_completed");
    }

    [Fact]
    public async Task Rating_Edit_Within_Window_Succeeds_And_Recomputes_Avg()
    {
        var r = await _svc.CreateAsync(_customerUserId, ValidCreate(), CancellationToken.None);
        await _svc.AdminAssignAsync(r.Data, new AssignDriverDto(_driverId), CancellationToken.None);
        await _svc.DriverStartAsync(_driverUserId, r.Data, CancellationToken.None);
        await _svc.DriverCompleteAsync(_driverUserId, r.Data, CancellationToken.None);
        await _rating.RateAsync(_customerUserId, r.Data, new RateReservationDto(2, null), CancellationToken.None);

        var edit = await _rating.EditAsync(_customerUserId, r.Data, new RateReservationDto(5, "updated"), CancellationToken.None);
        edit.Success.Should().BeTrue();
        var driver = await _db.Drivers.FirstAsync(d => d.Id == _driverId);
        driver.AverageRating.Should().Be(5m);
    }

    [Fact]
    public async Task Rating_Edit_After_24h_Is_Rejected()
    {
        var r = await _svc.CreateAsync(_customerUserId, ValidCreate(), CancellationToken.None);
        await _svc.AdminAssignAsync(r.Data, new AssignDriverDto(_driverId), CancellationToken.None);
        await _svc.DriverStartAsync(_driverUserId, r.Data, CancellationToken.None);
        await _svc.DriverCompleteAsync(_driverUserId, r.Data, CancellationToken.None);
        await _rating.RateAsync(_customerUserId, r.Data, new RateReservationDto(2, null), CancellationToken.None);

        _clock.UtcNow = DateTime.UtcNow.AddHours(25);
        var edit = await _rating.EditAsync(_customerUserId, r.Data, new RateReservationDto(5, null), CancellationToken.None);
        edit.Success.Should().BeFalse();
        edit.Code.Should().Be("rating.edit_window_expired");
    }

    [Fact]
    public async Task Inactive_Driver_Cannot_Be_Assigned()
    {
        var r = await _svc.CreateAsync(_customerUserId, ValidCreate(), CancellationToken.None);
        var driver = await _db.Drivers.FirstAsync(d => d.Id == _driverId);
        driver.SetStatus(DriverStatus.Inactive);
        await _db.SaveChangesAsync();

        var assign = await _svc.AdminAssignAsync(r.Data, new AssignDriverDto(_driverId), CancellationToken.None);
        assign.Success.Should().BeFalse();
        assign.Code.Should().Be("driver.inactive");
    }

    // ----------- Listing -----------

    [Fact]
    public async Task Customer_Lists_Only_Own_Reservations()
    {
        await _svc.CreateAsync(_customerUserId, ValidCreate(), CancellationToken.None);
        await _svc.CreateAsync(_customerUserId, ValidCreate(), CancellationToken.None);
        await _svc.CreateAsync(_otherCustomerUserId, ValidCreate(), CancellationToken.None);

        var mine = await _svc.ListOwnAsync(_customerUserId, null, CancellationToken.None);
        mine.Success.Should().BeTrue();
        mine.Data!.Items.Should().HaveCount(2);

        var others = await _svc.ListOwnAsync(_otherCustomerUserId, null, CancellationToken.None);
        others.Data!.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Admin_List_Paging_And_Status_Filter_Work()
    {
        for (int i = 0; i < 5; i++) await _svc.CreateAsync(_customerUserId, ValidCreate(), CancellationToken.None);
        var r = await _svc.CreateAsync(_customerUserId, ValidCreate(), CancellationToken.None);
        await _svc.CancelByCustomerAsync(_customerUserId, r.Data, CancellationToken.None);

        var all = await _svc.AdminListAsync(new ReservationListQuery(null, null, null, null, null, null, 1, 10), CancellationToken.None);
        all.Data!.TotalCount.Should().Be(6);

        var cancelled = await _svc.AdminListAsync(new ReservationListQuery(ReservationStatus.Cancelled, null, null, null, null, null, 1, 10), CancellationToken.None);
        cancelled.Data!.TotalCount.Should().Be(1);
    }

    // ----------- Helpers -----------

    private static CreateReservationDto ValidCreate() => new(
        ServiceType.Driver,
        DateTime.UtcNow.AddHours(3),
        "Kadıköy Moda Cd. No:5, İstanbul",
        Lat: 40.9875,
        Lng: 29.0254,
        Note: "Deneme not",
        PlaceSelectedFromAutocomplete: true);

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
}
