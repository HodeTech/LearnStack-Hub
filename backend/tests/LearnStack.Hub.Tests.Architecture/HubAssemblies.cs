using System.Reflection;

namespace LearnStack.Hub.Tests.Architecture;

/// <summary>
/// Central catalogue of the Hub assemblies the architecture tests scan. Each
/// entry is pinned via a real <c>AssemblyMarker</c> type reference so the
/// assembly is actually loaded (NetArchTest walks loaded assemblies' IL).
/// </summary>
internal static class HubAssemblies
{
    public static readonly Assembly SharedKernel = typeof(SharedKernel.AssemblyMarker).Assembly;

    // Per module: Domain / Application / Infrastructure (+ Application.Contracts).
    public static readonly Assembly TenantLifecycleDomain = typeof(Modules.TenantLifecycle.Domain.AssemblyMarker).Assembly;
    public static readonly Assembly TenantLifecycleApplication = typeof(Modules.TenantLifecycle.Application.AssemblyMarker).Assembly;
    public static readonly Assembly TenantLifecycleInfrastructure = typeof(Modules.TenantLifecycle.Infrastructure.AssemblyMarker).Assembly;

    public static readonly Assembly PlansDomain = typeof(Modules.Plans.Domain.AssemblyMarker).Assembly;
    public static readonly Assembly PlansApplication = typeof(Modules.Plans.Application.AssemblyMarker).Assembly;
    public static readonly Assembly PlansInfrastructure = typeof(Modules.Plans.Infrastructure.AssemblyMarker).Assembly;

    public static readonly Assembly SubscriptionsDomain = typeof(Modules.Subscriptions.Domain.AssemblyMarker).Assembly;
    public static readonly Assembly SubscriptionsApplication = typeof(Modules.Subscriptions.Application.AssemblyMarker).Assembly;
    public static readonly Assembly SubscriptionsInfrastructure = typeof(Modules.Subscriptions.Infrastructure.AssemblyMarker).Assembly;

    public static readonly Assembly EntitlementsDomain = typeof(Modules.Entitlements.Domain.AssemblyMarker).Assembly;
    public static readonly Assembly EntitlementsApplication = typeof(Modules.Entitlements.Application.AssemblyMarker).Assembly;
    public static readonly Assembly EntitlementsInfrastructure = typeof(Modules.Entitlements.Infrastructure.AssemblyMarker).Assembly;

    /// <summary>Every module Domain assembly.</summary>
    public static readonly IReadOnlyList<Assembly> ModuleDomains =
    [
        TenantLifecycleDomain,
        PlansDomain,
        SubscriptionsDomain,
        EntitlementsDomain,
    ];

    /// <summary>Every Hub module assembly across all layers.</summary>
    public static readonly IReadOnlyList<Assembly> AllModuleLayers =
    [
        TenantLifecycleDomain, TenantLifecycleApplication, TenantLifecycleInfrastructure,
        PlansDomain, PlansApplication, PlansInfrastructure,
        SubscriptionsDomain, SubscriptionsApplication, SubscriptionsInfrastructure,
        EntitlementsDomain, EntitlementsApplication, EntitlementsInfrastructure,
    ];

    /// <summary>A module's "other module" Domain namespaces — what its own Domain must never reference.</summary>
    public static IReadOnlyList<string> OtherModuleDomainNamespaces(string ownModule)
    {
        string[] all =
        [
            "LearnStack.Hub.Modules.TenantLifecycle.Domain",
            "LearnStack.Hub.Modules.Plans.Domain",
            "LearnStack.Hub.Modules.Subscriptions.Domain",
            "LearnStack.Hub.Modules.Entitlements.Domain",
        ];

        return all.Where(ns => !ns.Contains($".{ownModule}.", StringComparison.Ordinal)).ToArray();
    }
}
