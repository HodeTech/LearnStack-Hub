using LearnStack.Hub.Modules.TenantLifecycle.Application.Abstractions;
using LearnStack.Hub.Modules.TenantLifecycle.Application.Contracts;
using LearnStack.Hub.Modules.TenantLifecycle.Domain;
using LearnStack.Hub.SharedKernel.Hosting;
using LearnStack.Hub.SharedKernel.Identifiers;
using LearnStack.Hub.SharedKernel.Localization;
using LearnStack.Hub.SharedKernel.Results;
using LearnStack.Hub.SharedKernel.Time;
using MediatR;

namespace LearnStack.Hub.Modules.TenantLifecycle.Application.Handlers;

/// <summary>
/// Provisions a tenant. P02c-1c creates the <see cref="LearnStackTenant"/>
/// (Trial). The trial-subscription + initial-entitlement orchestration is wired
/// in P02c-1d once the Subscriptions + Entitlements contracts exist; the
/// <c>POST /api/internal/tenants</c> push to LearnStack core is P02c-3.
/// </summary>
public sealed class CreateTenantCommandHandler(
    ITenantRepository repository,
    IClock clock,
    IGuidFactory guids)
    : IRequestHandler<CreateTenantCommand, Result<TenantCreatedDto>>
{
    public async Task<Result<TenantCreatedDto>> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (await repository.SlugExistsAsync(request.Slug, cancellationToken).ConfigureAwait(false))
        {
            return Result<TenantCreatedDto>.Fail(new Error(new LocalizedMessage("lockey_business_rule_violation")));
        }

        var deploymentMode = Enum.Parse<DeploymentMode>(request.DeploymentMode, ignoreCase: true);

        var tenant = LearnStackTenant.Create(
            LearnStackTenantId.From(guids.NewUuidV7()),
            request.Slug,
            request.DisplayName,
            deploymentMode,
            clock,
            HubSystemActors.SystemOperator);

        await repository.AddAsync(tenant, cancellationToken).ConfigureAwait(false);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // TODO(P02c-1d): orchestrate the trial subscription + initial entitlement
        // recompute (generation 1) by sending StartTrialCommand (Subscriptions),
        // which triggers RecomputeEntitlementCommand (Entitlements).

        return Result<TenantCreatedDto>.Ok(
            new TenantCreatedDto(tenant.Id.Value, tenant.Slug, tenant.Status.ToString()));
    }
}
