using FluentAssertions;
using PickMe.Domain;
using PickMe.Domain.Entities;
using Xunit;

namespace PickMe.UnitTests.Domain;

public class UserLockoutTests
{
    private static User NewUser() => User.Create(Guid.NewGuid(), "x@y.tr", "hash", UserRole.Customer);

    [Fact]
    public void Fifth_Failed_Attempt_Locks_Account_For_15_Min()
    {
        var u = NewUser();
        for (int i = 0; i < 5; i++) u.RecordFailedLogin(5, 15);
        u.IsLocked(DateTime.UtcNow).Should().BeTrue();
        u.IsLocked(DateTime.UtcNow.AddMinutes(16)).Should().BeFalse();
    }

    [Fact]
    public void Fourth_Failure_Does_Not_Lock_Account()
    {
        var u = NewUser();
        for (int i = 0; i < 4; i++) u.RecordFailedLogin(5, 15);
        u.IsLocked(DateTime.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void Successful_Login_Resets_Counter_And_Unlocks()
    {
        var u = NewUser();
        for (int i = 0; i < 5; i++) u.RecordFailedLogin(5, 15);
        u.IsLocked(DateTime.UtcNow).Should().BeTrue();
        u.RecordSuccessfulLogin();
        u.IsLocked(DateTime.UtcNow).Should().BeFalse();
        u.FailedLoginAttempts.Should().Be(0);
        u.LastLoginAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Email_Is_Stored_Lowercase()
    {
        var u = User.Create(Guid.NewGuid(), "  Ali@Example.TR  ", "h", UserRole.Admin);
        u.Email.Should().Be("ali@example.tr");
    }
}
