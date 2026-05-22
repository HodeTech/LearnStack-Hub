using FluentAssertions;
using LearnStack.Hub.SharedKernel.Localization;
using Xunit;

namespace LearnStack.Hub.Tests.Unit.SharedKernel;

public sealed class LocalizedMessageTests
{
    [Fact]
    public void Ctor_RejectsKeyWithoutLockeyPrefix()
    {
        var act = () => new LocalizedMessage("not_found");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Ctor_AcceptsPrefixedKey()
    {
        var message = new LocalizedMessage("lockey_not_found");

        message.Key.Should().Be("lockey_not_found");
        message.Params.Should().BeNull();
    }

    [Fact]
    public void Equality_IsStructural_OverParams()
    {
        var a = new LocalizedMessage("lockey_x", new Dictionary<string, string> { ["slug"] = "acme" });
        var b = new LocalizedMessage("lockey_x", new Dictionary<string, string> { ["slug"] = "acme" });

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void EmptyParams_NormaliseToNull()
    {
        var message = new LocalizedMessage("lockey_x", new Dictionary<string, string>());

        message.Params.Should().BeNull();
    }
}
