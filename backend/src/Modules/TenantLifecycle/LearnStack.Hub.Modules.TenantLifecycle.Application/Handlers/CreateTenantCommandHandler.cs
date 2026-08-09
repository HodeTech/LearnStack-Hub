using LearnStack.Hub.Modules.Subscriptions.Application.Contracts;
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
/// Provisions a tenant: creates the <see cref="LearnStackTenant"/> (Trial), then
/// orchestrates the initial trial subscription (StartTrialCommand into the
/// Subscriptions module), which in turn triggers the first entitlement recompute
/// (generation 1). All within the one outer transaction. The
/// <c>POST /api/internal/tenants</c> push to LearnStack core is P02c-3.
/// </summary>
public sealed class CreateTenantCommandHandler(
    ITenantRepository repository,
    IMediator mediator,
    IClock clock,
    IGuidFactory guids)
    : IRequestHandler<CreateTenantCommand, Result<TenantCreatedDto>>
{
    private const int TrialDays = 14;

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

        // Start the trial subscription; the Subscriptions handler triggers the
        // initial entitlement recompute (generation 1). A failure rolls back the
        // whole outer transaction (the tenant insert included).
        var trial = await mediator.Send(
            new StartTrialCommand(tenant.Id.Value, request.InitialPlanId, TrialDays),
            cancellationToken).ConfigureAwait(false);
        if (trial.IsFailure)
        {
            return Result<TenantCreatedDto>.Fail(trial.Error!);
        }

        return Result<TenantCreatedDto>.Ok(
            new TenantCreatedDto(tenant.Id.Value, tenant.Slug, tenant.Status.ToString()));
    }
}
