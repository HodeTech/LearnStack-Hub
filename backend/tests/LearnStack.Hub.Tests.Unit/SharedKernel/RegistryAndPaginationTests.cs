using FluentAssertions;
using LearnStack.Hub.SharedKernel.FeatureFlags;
using LearnStack.Hub.SharedKernel.Pagination;
using Xunit;

namespace LearnStack.Hub.Tests.Unit.SharedKernel;

public sealed class RegistryAndPaginationTests
{
    [Fact]
    public void FeatureKeys_AreDottedSnakeCase_WithoutEnabledSuffix()
    {
        foreach (var key in FeatureKeys.All)
        {
            key.Value.Should().MatchRegex("^[a-z0-9]+(\\.[a-z0-9_]+)+$");
            key.Value.Should().NotEndWith(".enabled");
        }
    }

    [Fact]
    public void LimitKeys_CarryLimitsPrefix()
    {
        foreach (var key in LimitKeys.All)
        {
            key.Value.Should().StartWith("limits.");
        }
    }

    [Fact]
    public void FeatureKeys_IsKnown_MatchesRegistry()
    {
        FeatureKeys.IsKnown("classroom.recording").Should().BeTrue();
        FeatureKeys.IsKnown("nonsense.key").Should().BeFalse();
    }

    [Fact]
    public void LimitKeys_IsKnown_MatchesRegistry()
    {
        LimitKeys.IsKnown("limits.max_users").Should().BeTrue();
        LimitKeys.IsKnown("max_users").Should().BeFalse();
    }

    [Fact]
    public void CursorPagination_ClampsAboveMax()
    {
        var page = new CursorPagination(Limit: 5000);

        page.Limit.Should().Be(CursorPagination.MaxLimit);
    }

    [Fact]
    public void CursorPagination_RejectsNonPositiveLimit()
    {
        var act = () => new CursorPagination(Limit: 0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
