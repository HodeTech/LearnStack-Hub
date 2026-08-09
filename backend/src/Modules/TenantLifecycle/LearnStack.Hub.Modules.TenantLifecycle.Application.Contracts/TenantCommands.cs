using LearnStack.Hub.SharedKernel.Results;
using MediatR;

namespace LearnStack.Hub.Modules.TenantLifecycle.Application.Contracts;

/// <summary>
/// Provisions a tenant: creates the <c>LearnStackTenant</c> (Trial), the
/// initial trial <c>HubSubscription</c> bound to <see cref="InitialPlanId"/>,
/// and the first <c>Entitlement</c> (generation 1). The
/// <c>POST /api/internal/tenants</c> push to LearnStack core is P02c-3.
/// </summary>
public sealed record CreateTenantCommand(
    string Slug,
    string DisplayName,
    string DeploymentMode,
    Guid InitialPlanId) : IRequest<Result<TenantCreatedDto>>;

/// <summary><c>Trial | Suspended → Active</c>; triggers an entitlement recompute.</summary>
public sealed record ActivateTenantCommand(Guid TenantId) : IRequest<Result<Unit>>;

/// <summary><c>Active → Suspended</c>; triggers an entitlement recompute.</summary>
public sealed record SuspendTenantCommand(Guid TenantId, string Reason) : IRequest<Result<Unit>>;

/// <summary><c>Active | Suspended → Archived</c>.</summary>
public sealed record ArchiveTenantCommand(Guid TenantId) : IRequest<Result<Unit>>;
