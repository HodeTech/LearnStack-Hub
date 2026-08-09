using FluentAssertions;
using LearnStack.Hub.SharedKernel.Localization;
using LearnStack.Hub.SharedKernel.Results;
using Xunit;

namespace LearnStack.Hub.Tests.Unit.SharedKernel;

public sealed class ResultTests
{
    [Fact]
    public void Ok_WrapsValue()
    {
        var result = Result<int>.Ok(42);

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Value.Should().Be(42);
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Ok_ThrowsOnNullValue()
    {
        var act = () => Result<string>.Ok(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Fail_CarriesError()
    {
        var error = new Error(new LocalizedMessage("lockey_not_found"));

        var result = Result<int>.Fail(error);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
        result.Error!.Code.Should().Be("not_found");
    }

    [Fact]
    public void FailFor_BuildsConcreteResultShape()
    {
        var error = new Error(new LocalizedMessage("lockey_validation_failed"));

        var result = Result.FailFor<Result<string>>(error);

        result.Should().BeOfType<Result<string>>();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void Error_Code_StripsLockeyPrefix()
    {
        var error = new Error(new LocalizedMessage("lockey_business_rule_violation"));

        error.Code.Should().Be("business_rule_violation");
    }
}
