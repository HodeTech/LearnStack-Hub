using LearnStack.Hub.Modules.Entitlements.Application.Abstractions;
using LearnStack.Hub.Modules.Entitlements.Application.Contracts;
using LearnStack.Hub.Modules.Entitlements.Domain;
using LearnStack.Hub.Modules.Plans.Application.Contracts;
using LearnStack.Hub.Modules.Subscriptions.Application.Contracts;
using LearnStack.Hub.SharedKernel.Compliance;
using LearnStack.Hub.SharedKernel.Identifiers;
using LearnStack.Hub.SharedKernel.Results;
using LearnStack.Hub.SharedKernel.Time;
using MediatR;

namespace LearnStack.Hub.Modules.Entitlements.Application;

/// <summary>
/// Rebuilds the <see cref="Entitlement"/> projection from <c>Plan</c> +
/// <c>HubSubscription</c> (+ <c>CompliancePolicy</c> from P02c-5). Reads both
/// inputs via Application.Contracts queries (never their Domain), upserts the
/// projection in its own DbContext within the active transaction, and bumps the
/// monotonic generation. In P02c-1 the <c>learnstack.hub.entitlement</c> Dapr
/// publish is a no-op shell — the in-process recompute + persist is the deliverable.
/// </summary>
public sealed class EntitlementProjectionService(
    IEntitlementRepository repository,
    IMediator mediator,
    IClock clock)
{
    public async Task<Result<EntitlementProjectionDto>> RecomputeAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var subscription = await mediator.Send(new GetSubscriptionQuery(tenantId), cancellationToken).ConfigureAwait(false);
        if (subscription.IsFailure)
        {
            return Result<EntitlementProjectionDto>.Fail(subscription.Error!);
        }

        var plan = await mediator.Send(new GetPlanQuery(subscription.Value!.PlanId), cancellationToken).ConfigureAwait(false);
        if (plan.IsFailure)
        {
            return Result<EntitlementProjectionDto>.Fail(plan.Error!);
        }

        var sub = subscription.Value!;
        var p = plan.Value!;

        // Compose. compliance.caps is empty in P02c-1 (CompliancePolicy is P02c-5);
        // grace_until is null until license/dunning (P02c-6 / Phase 09b).
        var caps = new Dictionary<string, ComplianceCap>(StringComparer.Ordinal);
        var tenantStronglyTyped = LearnStackTenantId.From(tenantId);

        var existing = await repository.GetByTenantAsync(tenantStronglyTyped, cancellationToken).ConfigureAwait(false);
        Entitlement entitlement;
        if (existing is null)
        {
            entitlement = Entitlement.CreateInitial(
                tenantStronglyTyped,
                p.Tier,
                p.Features,
                p.Limits,
                caps,
                sub.CurrentPeriodEnd,
                graceUntil: null,
                clock);
            await repository.AddAsync(entitlement, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            existing.Recompute(
                p.Tier,
                p.Features,
                p.Limits,
                caps,
                sub.CurrentPeriodEnd,
                graceUntil: null,
                clock);
            entitlement = existing;
        }

        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // TODO(P02c-2): enqueue the learnstack.hub.entitlement integration event
        // via IOutbox carrying { tenant_id, generation, expires_at }.

        return Result<EntitlementProjectionDto>.Ok(entitlement.ToProjectionDto());
    }
}
