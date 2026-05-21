using FluentAssertions;
using LearnStack.Hub.SharedKernel;
using Xunit;

namespace LearnStack.Hub.Tests.Unit;

/// <summary>
/// Placeholder smoke tests so the unit-test project is non-empty in P02c-0.
/// Real unit tests against Hub aggregates land in P02c-1+.
/// </summary>
public sealed class SmokeTests
{
    [Fact]
    public void SharedKernel_AssemblyMarker_Loads()
    {
        var marker = typeof(AssemblyMarker);

        marker.Should().NotBeNull();
        marker.Assembly.GetName().Name.Should().Be("LearnStack.Hub.SharedKernel");
    }
}
