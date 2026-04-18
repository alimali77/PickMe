using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace PickMe.ArchitectureTests;

public class LayeringTests
{
    private const string DomainNs = "PickMe.Domain";
    private const string ApplicationNs = "PickMe.Application";
    private const string InfrastructureNs = "PickMe.Infrastructure";
    private const string ApiNs = "PickMe.Api";

    [Fact]
    public void Domain_Should_Not_Depend_On_Any_Other_Layer()
    {
        var result = Types.InAssembly(typeof(PickMe.Domain.Entities.User).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(ApplicationNs, InfrastructureNs, ApiNs)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Application_Should_Not_Depend_On_Infrastructure_Or_Api()
    {
        var result = Types.InAssembly(typeof(PickMe.Application.AssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(InfrastructureNs, ApiNs)
            .GetResult();

        result.IsSuccessful.Should().BeTrue($"Application leaked: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Infrastructure_Should_Not_Depend_On_Api()
    {
        var result = Types.InAssembly(typeof(PickMe.Infrastructure.Persistence.ApplicationDbContext).Assembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNs)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }
}
