using FluentAssertions;
using LearnStack.Hub.Application.Pipeline;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LearnStack.Hub.Tests.Architecture;

/// <summary>
/// Asserts the Hub MediatR pipeline is the canonical <strong>six-behavior</strong>
/// sequence — LearnStack core's seven minus <c>TenantContextBehavior</c> — in
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

    /// <summary>
    /// The declared list is only a claim until <see cref="MediatRPipelineRegistration.AddHubMediatRPipeline"/>
    /// actually registers it. MediatR resolves open-generic
    /// <see cref="IPipelineBehavior{TRequest,TResponse}"/> descriptors in registration
    /// order — outermost first — so the registered order *is* the execution order.
    /// </summary>
    [Fact]
    public void AddHubMediatRPipeline_Registers_The_Canonical_Order()
    {
        var services = new ServiceCollection();

        services.AddHubMediatRPipeline(typeof(LearnStack.Hub.Application.AssemblyMarker).Assembly);

        var registered = services
            .Where(d => d.ServiceType == typeof(IPipelineBehavior<,>))
            .Select(d => d.ImplementationType!)
            .ToArray();

        // The registered pipeline — not the declared list — is what actually runs.
        registered.Should().Equal(MediatRPipelineRegistration.CanonicalBehaviorOrder);
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
