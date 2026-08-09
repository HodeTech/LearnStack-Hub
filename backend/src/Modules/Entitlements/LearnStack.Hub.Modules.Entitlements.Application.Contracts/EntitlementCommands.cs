using LearnStack.Hub.SharedKernel.Results;
using MediatR;

namespace LearnStack.Hub.Modules.Entitlements.Application.Contracts;

/// <summary>
/// Rebuilds the tenant's entitlement projection from <c>Plan</c> +
/// <c>HubSubscription</c> and bumps the monotonic generation. The public entry
/// the Subscriptions / Plans / TenantLifecycle handlers call after a state change.
/// </summary>
public sealed record RecomputeEntitlementCommand(Guid TenantId) : IRequest<Result<EntitlementProjectionDto>>;

/// <summary>Reads the current entitlement projection for a tenant.</summary>
public sealed record GetEntitlementQuery(Guid TenantId) : IRequest<Result<EntitlementProjectionDto>>;
