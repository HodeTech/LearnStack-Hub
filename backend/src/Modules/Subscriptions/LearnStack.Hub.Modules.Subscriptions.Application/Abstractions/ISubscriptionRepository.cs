using LearnStack.Hub.Modules.Subscriptions.Domain;
using LearnStack.Hub.SharedKernel.Identifiers;

namespace LearnStack.Hub.Modules.Subscriptions.Application.Abstractions;

/// <summary>Persistence port for the <see cref="HubSubscription"/> aggregate (1:1 with tenant).</summary>
public interface ISubscriptionRepository
{
    Task<HubSubscription?> GetByTenantAsync(LearnStackTenantId tenantId, CancellationToken cancellationToken);

    Task<bool> ExistsForTenantAsync(LearnStackTenantId tenantId, CancellationToken cancellationToken);

    /// <summary>Tenant ids of every subscription bound to the given plan (for the Plans fan-out recompute).</summary>
    Task<IReadOnlyList<LearnStackTenantId>> GetTenantIdsByPlanAsync(Guid planId, CancellationToken cancellationToken);

    Task<IReadOnlyList<HubSubscription>> ListAsync(
        SubscriptionStatus? status,
        Guid? afterId,
        int limitPlusOne,
        CancellationToken cancellationToken);

    Task AddAsync(HubSubscription subscription, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
