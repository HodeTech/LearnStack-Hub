using FluentAssertions;
using LearnStack.Hub.SharedKernel.Hosting;
using Xunit;

namespace LearnStack.Hub.Tests.Unit.SharedKernel;

/// <summary>
/// The composition root branches its error-tracking and resilience providers on
/// <see cref="DeploymentMode"/>, so a value that resolves to the wrong member —
/// or silently to Development — is a production misconfiguration nobody sees.
/// These are the regression cases for that.
/// </summary>
public sealed class DeploymentModeResolverTests
{
    [Theory]
    [InlineData("SaaS", DeploymentMode.SaaS)]
    [InlineData("saas", DeploymentMode.SaaS)]
    [InlineData("SELFHOSTEDAIRGAPPED", DeploymentMode.SelfHostedAirGapped)]
    [InlineData("  Dedicated  ", DeploymentMode.Dedicated)]
    public void Resolve_Accepts_A_Member_Name_Case_Insensitively_And_Trimmed(
        string configured, DeploymentMode expected)
    {
        DeploymentModeResolver.Resolve(configured, isDevelopmentEnvironment: false)
            .Should().Be(expected);
    }

    /// <summary>
    /// <c>Enum.TryParse</c> would accept every one of these. "3" lands on
    /// SelfHostedOnline and "SaaS,Dedicated" ORs to 3 and lands there too, both
    /// passing an <c>Enum.IsDefined</c> guard while meaning nothing an operator
    /// wrote on purpose.
    /// </summary>
    [Theory]
    [InlineData("3")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("SaaS,Dedicated")]
    [InlineData("SaaS, Dedicated")]
    [InlineData("Production")]
    [InlineData("Self-Hosted-Online")]
    public void Resolve_Rejects_Numeric_Combined_And_Unknown_Values(string configured)
    {
        var resolve = () => DeploymentModeResolver.Resolve(configured, isDevelopmentEnvironment: false);

        resolve.Should().Throw<InvalidOperationException>()
            .WithMessage("*Hub:DeploymentMode*");
    }

    [Theory]
    [InlineData("3")]
    [InlineData("SaaS,Dedicated")]
    public void Resolve_Rejects_Invalid_Values_In_Development_Too(string configured)
    {
        var resolve = () => DeploymentModeResolver.Resolve(configured, isDevelopmentEnvironment: true);

        resolve.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_Throws_When_Missing_Outside_Development(string? configured)
    {
        var resolve = () => DeploymentModeResolver.Resolve(configured, isDevelopmentEnvironment: false);

        resolve.Should().Throw<InvalidOperationException>()
            .WithMessage("*required outside the Development environment*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_Defaults_To_Development_Only_When_Missing_In_Development(string? configured)
    {
        DeploymentModeResolver.Resolve(configured, isDevelopmentEnvironment: true)
            .Should().Be(DeploymentMode.Development);
    }
}
