using FluentAssertions;
using LearnStack.Hub.Application.Pipeline;
using Xunit;

namespace LearnStack.Hub.Tests.Architecture;

/// <summary>
/// Asserts the Hub MediatR pipeline is the canonical <strong>6-step</strong>
/// sequence (LearnStack core's 8 minus the two tenant-isolation steps), in
/// order — Validation → Logging → AuditLog → Authorization → Transaction →
/// OutboxFlush. No TenantContextBehavior. See cross-cutting-foundation.md § 2.
/// </summary>
public sealed class PipelineOrderTests
{
    [Fact]
    public void MediatR_Pipeline_Order_Matches_Canonical_Sequence()
    {
        var expected = new[]
        {
            typeof(ValidationBehavior<,>),
            typeof(LoggingBehavior<,>),
            typeof(AuditLogBehavior<,>),
            typeof(AuthorizationBehavior<,>),
            typeof(TransactionBehavior<,>),
            typeof(OutboxFlushBehavior<,>),
        };

        MediatRPipelineRegistration.CanonicalBehaviorOrder.Should().Equal(expected);
    }

    [Fact]
    public void Pipeline_Has_No_TenantContextBehavior()
    {
        MediatRPipelineRegistration.CanonicalBehaviorOrder
            .Select(t => t.Name)
            .Should()
            .NotContain(name => name.Contains("TenantContext", StringComparison.Ordinal),
                "Hub is operator-administered, not tenant-isolated — there is no TenantContextBehavior.");
    }
}
