using LearnStack.Hub.Modules.Subscriptions.Application.Abstractions;
using LearnStack.Hub.Modules.Subscriptions.Domain;
using LearnStack.Hub.SharedKernel.Identifiers;
using Microsoft.EntityFrameworkCore;

namespace LearnStack.Hub.Modules.Subscriptions.Infrastructure.Persistence;

internal sealed class SubscriptionRepository(SubscriptionsDbContext db) : ISubscriptionRepository
{
    public async Task<HubSubscription?> GetByTenantAsync(LearnStackTenantId tenantId, CancellationToken cancellationToken) =>
        await db.Subscriptions.FirstOrDefaultAsync(s => s.TenantId == tenantId, cancellationToken).ConfigureAwait(false);

    public async Task<bool> ExistsForTenantAsync(LearnStackTenantId tenantId, CancellationToken cancellationToken) =>
        await db.Subscriptions.AnyAsync(s => s.TenantId == tenantId, cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<LearnStackTenantId>> GetTenantIdsByPlanAsync(Guid planId, CancellationToken cancellationToken) =>
        await db.Subscriptions
            .AsNoTracking()
            .Where(s => s.PlanId == planId)
            .Select(s => s.TenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<HubSubscription>> ListAsync(
        SubscriptionStatus? status,
        Guid? afterId,
        int limitPlusOne,
        CancellationToken cancellationToken)
    {
        var query = db.Subscriptions.AsNoTracking();
        if (status is { } s)
        {
            query = query.Where(x => x.Status == s);
        }

        // P02c-1 keyset slice in memory (keyed on tenant id) — subscription
        // volume is tiny. SQL keyset replaces this when volume warrants.
        var rows = await query.ToListAsync(cancellationToken).ConfigureAwait(false);
        var ordered = rows
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.TenantId.Value)
            .ToList();

        IEnumerable<HubSubscription> page = ordered;
        if (afterId is { } cursor)
        {
            var index = ordered.FindIndex(x => x.TenantId.Value == cursor);
            page = index >= 0 ? ordered.Skip(index + 1) : ordered;
        }

        return page.Take(limitPlusOne).ToList();
    }

    public async Task AddAsync(HubSubscription subscription, CancellationToken cancellationToken) =>
        await db.Subscriptions.AddAsync(subscription, cancellationToken).ConfigureAwait(false);

    public async Task SaveChangesAsync(CancellationToken cancellationToken) =>
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
}
