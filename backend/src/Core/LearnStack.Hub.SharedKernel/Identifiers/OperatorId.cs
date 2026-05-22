using Vogen;

namespace LearnStack.Hub.SharedKernel.Identifiers;

/// <summary>
/// Cross-cutting strongly-typed identifier for a Hub <em>operator</em> — a
/// LearnStack staff member acting in the <c>learnstack-hub</c> Keycloak realm.
/// This is the load-bearing Hub adjustment versus LearnStack core: Hub's
/// cross-cutting actor id is <see cref="OperatorId"/>, NOT a tenant <c>UserId</c>.
/// </summary>
/// <remarks>
/// There is deliberately no tenant <c>UserId</c> type anywhere in Hub. Hub
/// references tenants by <c>LearnStackTenantId</c>, never tenant users by id —
/// a tenant <c>UserId</c> appearing in Hub would be a
/// <c>Hub_NeverStores_TenantData</c>-adjacent smell. Audit columns
/// (<c>CreatedBy</c> / <c>UpdatedBy</c> / <c>DeletedBy</c>) and
/// <c>CapturedContext</c> are all typed on <see cref="OperatorId"/>.
/// </remarks>
[ValueObject<Guid>(LearnStackHubVogenDefaults.IdMask)]
public readonly partial record struct OperatorId : IStronglyTypedId<Guid>
{
}
