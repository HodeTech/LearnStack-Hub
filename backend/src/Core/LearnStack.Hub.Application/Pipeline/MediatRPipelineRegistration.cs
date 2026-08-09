using System.Reflection;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace LearnStack.Hub.Application.Pipeline;

/// <summary>
/// Composition-root extension that registers the canonical <strong>6-step</strong>
/// Hub MediatR pipeline. Hub drops the two tenant-isolation steps LearnStack
/// core's 8-step pipeline carries (<c>TenantContextBehavior</c> and the
/// tenant-scoped concerns) because Hub is operator-administered, not
/// tenant-isolated. Outermost (validation) first, innermost (handler) last; the
/// <c>MediatR_Pipeline_Order_Matches_Canonical_Sequence</c> architecture test
/// asserts this DI registration order.
/// </summary>
public static class MediatRPipelineRegistration
{
    /// <summary>
    /// The 6 pipeline behaviors in canonical order. The handler is the seventh
    /// (innermost) step, resolved by MediatR itself. Do not reorder without
    /// amending <c>docs/architecture/cross-cutting-foundation.md § 2</c>.
    /// </summary>
    public static IReadOnlyList<Type> CanonicalBehaviorOrder { get; } =
    [
        typeof(ValidationBehavior<,>),
        typeof(LoggingBehavior<,>),
        typeof(AuditLogBehavior<,>),
        typeof(AuthorizationBehavior<,>),
        typeof(TransactionBehavior<,>),
        typeof(OutboxFlushBehavior<,>),
        // Step 7 (the handler) is resolved by MediatR itself.
    ];

    /// <summary>
    /// Registers the 6-step MediatR pipeline against <paramref name="services"/>.
    /// Handler types are scanned from <paramref name="handlerAssemblies"/>
    /// (typically each module's <c>AssemblyMarker</c> assembly).
    /// </summary>
    public static IServiceCollection AddHubMediatRPipeline(
        this IServiceCollection services,
        params Assembly[] handlerAssemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(handlerAssemblies);

        var assembliesToScan = handlerAssemblies.Length > 0
            ? handlerAssemblies
            : [typeof(AssemblyMarker).Assembly];

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(assembliesToScan);

            foreach (var behaviorType in CanonicalBehaviorOrder)
            {
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), behaviorType);
            }
        });

        return services;
    }
}
