using FluentAssertions;
using PickMe.Domain;
using PickMe.Domain.Common;
using PickMe.Domain.Entities;
using Xunit;

namespace PickMe.UnitTests.Domain;

public class ReservationStateMachineTests
{
    private static Reservation NewPending() => Reservation.Create(
        Guid.NewGuid(),
        Guid.NewGuid(),
        ServiceType.Driver,
        DateTime.UtcNow.AddHours(2),
        "Kadıköy, İstanbul",
        40.99,
        29.02,
        null);

    // ------ Valid transitions ------

    [Fact]
    public void Pending_To_Assigned_Is_Valid()
    {
        var r = NewPending();
        var driverId = Guid.NewGuid();
        r.AssignDriver(driverId);
        r.Status.Should().Be(ReservationStatus.Assigned);
        r.DriverId.Should().Be(driverId);
        r.AssignedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Assigned_To_OnTheWay_Is_Valid()
    {
        var r = NewPending();
        r.AssignDriver(Guid.NewGuid());
        r.StartTrip();
        r.Status.Should().Be(ReservationStatus.OnTheWay);
        r.StartedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void OnTheWay_To_Completed_Is_Valid()
    {
        var r = NewPending();
        r.AssignDriver(Guid.NewGuid());
        r.StartTrip();
        r.CompleteTrip();
        r.Status.Should().Be(ReservationStatus.Completed);
        r.CompletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Assigned_Can_Be_Reassigned_To_Another_Driver()
    {
        var r = NewPending();
        var firstDriver = Guid.NewGuid();
        r.AssignDriver(firstDriver);
        var secondDriver = Guid.NewGuid();
        r.AssignDriver(secondDriver);
        r.DriverId.Should().Be(secondDriver);
        r.Status.Should().Be(ReservationStatus.Assigned);
    }

    [Fact]
    public void Customer_Can_Cancel_Only_When_Pending()
    {
        var r = NewPending();
        r.CancelByCustomer();
        r.Status.Should().Be(ReservationStatus.Cancelled);
        r.CancelledBy.Should().Be(CancelledBy.Customer);
    }

    [Fact]
    public void Admin_Can_Cancel_Pending_With_Reason()
    {
        var r = NewPending();
        r.CancelByAdmin("Müşteri aramıyor.");
        r.Status.Should().Be(ReservationStatus.Cancelled);
        r.CancellationReason.Should().Be("Müşteri aramıyor.");
    }

    [Fact]
    public void Admin_Can_Cancel_Assigned()
    {
        var r = NewPending();
        r.AssignDriver(Guid.NewGuid());
        r.CancelByAdmin("Şoför uygun değil.");
        r.Status.Should().Be(ReservationStatus.Cancelled);
    }

    [Fact]
    public void Admin_Can_Cancel_OnTheWay()
    {
        var r = NewPending();
        r.AssignDriver(Guid.NewGuid());
        r.StartTrip();
        r.CancelByAdmin("Müşteri iptal istedi.");
        r.Status.Should().Be(ReservationStatus.Cancelled);
    }

    // ------ Invalid transitions ------

    [Fact]
    public void Cannot_Start_Trip_From_Pending()
    {
        var r = NewPending();
        FluentActions.Invoking(() => r.StartTrip())
            .Should().Throw<InvalidStateTransitionException>();
    }

    [Fact]
    public void Cannot_Complete_From_Pending()
    {
        var r = NewPending();
        FluentActions.Invoking(() => r.CompleteTrip())
            .Should().Throw<InvalidStateTransitionException>();
    }

    [Fact]
    public void Cannot_Complete_From_Assigned()
    {
        var r = NewPending();
        r.AssignDriver(Guid.NewGuid());
        FluentActions.Invoking(() => r.CompleteTrip())
            .Should().Throw<InvalidStateTransitionException>();
    }

    [Fact]
    public void Cannot_Start_Trip_From_OnTheWay()
    {
        var r = NewPending();
        r.AssignDriver(Guid.NewGuid());
        r.StartTrip();
        FluentActions.Invoking(() => r.StartTrip())
            .Should().Throw<InvalidStateTransitionException>();
    }

    [Fact]
    public void Cannot_Assign_After_Completed()
    {
        var r = NewPending();
        r.AssignDriver(Guid.NewGuid());
        r.StartTrip();
        r.CompleteTrip();
        FluentActions.Invoking(() => r.AssignDriver(Guid.NewGuid()))
            .Should().Throw<InvalidStateTransitionException>();
    }

    [Fact]
    public void Customer_Cannot_Cancel_After_Assigned()
    {
        var r = NewPending();
        r.AssignDriver(Guid.NewGuid());
        FluentActions.Invoking(() => r.CancelByCustomer())
            .Should().Throw<InvalidStateTransitionException>();
    }

    [Fact]
    public void Customer_Cannot_Cancel_OnTheWay()
    {
        var r = NewPending();
        r.AssignDriver(Guid.NewGuid());
        r.StartTrip();
        FluentActions.Invoking(() => r.CancelByCustomer())
            .Should().Throw<InvalidStateTransitionException>();
    }

    [Fact]
    public void Admin_Cannot_Cancel_Completed()
    {
        var r = NewPending();
        r.AssignDriver(Guid.NewGuid());
        r.StartTrip();
        r.CompleteTrip();
        FluentActions.Invoking(() => r.CancelByAdmin("late"))
            .Should().Throw<InvalidStateTransitionException>();
    }

    [Fact]
    public void Admin_Cannot_Cancel_Twice()
    {
        var r = NewPending();
        r.CancelByAdmin("first");
        FluentActions.Invoking(() => r.CancelByAdmin("second"))
            .Should().Throw<InvalidStateTransitionException>();
    }

    [Fact]
    public void Admin_Cancel_Requires_Non_Empty_Reason()
    {
        var r = NewPending();
        FluentActions.Invoking(() => r.CancelByAdmin("   "))
            .Should().Throw<DomainException>()
            .WithMessage("İptal sebebi zorunludur.");
    }

    [Fact]
    public void Start_After_Cancelled_Throws()
    {
        var r = NewPending();
        r.CancelByCustomer();
        FluentActions.Invoking(() => r.StartTrip())
            .Should().Throw<InvalidStateTransitionException>();
    }
}
