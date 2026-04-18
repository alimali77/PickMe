using FluentAssertions;
using PickMe.Domain.Common;
using PickMe.Domain.Entities;
using Xunit;

namespace PickMe.UnitTests.Domain;

public class RatingTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void Valid_Score_Creates_Rating(int score)
    {
        var r = Rating.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), score, null);
        r.Score.Should().Be(score);
        r.IsFlagged.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    [InlineData(10)]
    public void Invalid_Score_Throws(int score)
    {
        FluentActions.Invoking(() => Rating.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), score, null))
            .Should().Throw<DomainException>()
            .Which.Code.Should().Be("rating.score_out_of_range");
    }

    [Fact]
    public void Edit_Within_24h_Is_Allowed()
    {
        var r = Rating.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 3, "ok");
        r.Edit(4, "daha iyi", DateTime.UtcNow.AddHours(23), 24);
        r.Score.Should().Be(4);
        r.Comment.Should().Be("daha iyi");
    }

    [Fact]
    public void Edit_After_24h_Throws()
    {
        var r = Rating.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 3, "ok");
        FluentActions.Invoking(() => r.Edit(4, "yeni", DateTime.UtcNow.AddHours(25), 24))
            .Should().Throw<DomainException>()
            .Which.Code.Should().Be("rating.edit_window_expired");
    }

    [Fact]
    public void Flag_Sets_Flag_And_Reason()
    {
        var r = Rating.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, "hakaret");
        r.Flag("Uygunsuz dil.");
        r.IsFlagged.Should().BeTrue();
        r.FlaggedReason.Should().Be("Uygunsuz dil.");
    }
}
