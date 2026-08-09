using Vogen;

namespace LearnStack.Hub.SharedKernel.Identifiers;

/// <summary>
/// Strongly-typed identifier for a LearnStack tenant — the Hub-side mirror of
/// the LearnStack-core <c>tenant.id</c> (same UUID on both sides). It lives in
/// the SharedKernel (like <see cref="OperatorId"/>) because it is referenced
/// across modules: it is the TenantLifecycle aggregate's id, the Subscriptions
/// foreign key, and the Entitlement projection's primary key. Cross-module
/// references to a tenant use this id; other cross-module FKs (e.g. a plan id)
/// stay plain <see cref="Guid"/> columns per the module-boundary rules.
/// </summary>
[ValueObject<Guid>(LearnStackHubVogenDefaults.IdMask)]
public readonly partial record struct LearnStackTenantId : IStronglyTypedId<Guid>
{
}
