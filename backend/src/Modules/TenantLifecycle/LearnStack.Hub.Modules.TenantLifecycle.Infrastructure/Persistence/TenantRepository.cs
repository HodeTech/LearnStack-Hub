using LearnStack.Hub.Modules.TenantLifecycle.Application.Abstractions;
using LearnStack.Hub.Modules.TenantLifecycle.Domain;
using LearnStack.Hub.SharedKernel.Identifiers;
using Microsoft.EntityFrameworkCore;

namespace LearnStack.Hub.Modules.TenantLifecycle.Infrastructure.Persistence;

internal sealed class TenantRepository(TenantLifecycleDbContext db) : ITenantRepository
{
    public async Task<LearnStackTenant?> GetByIdAsync(LearnStackTenantId id, CancellationToken cancellationToken) =>
        await db.Tenants.FirstOrDefaultAsync(t => t.Id == id, cancellationToken).ConfigureAwait(false);

    public async Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken) =>
        await db.Tenants.AnyAsync(t => t.Slug == slug, cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<LearnStackTenant>> ListAsync(
        TenantStatus? status,
        Guid? afterId,
        int limitPlusOne,
        CancellationToken cancellationToken)
    {
        var query = db.Tenants.AsNoTracking();
        if (status is { } s)
        {
            query = query.Where(t => t.Status == s);
        }

        // P02c-1 keyset slice in memory — tenant volume is tiny. SQL keyset
        // (ORDER BY ... WHERE id > cursor) replaces this when volume warrants.
        var rows = await query.ToListAsync(cancellationToken).ConfigureAwait(false);
        var ordered = rows
            .OrderBy(t => t.CreatedAt)
            .ThenBy(t => t.Id.Value)
            .ToList();

        IEnumerable<LearnStackTenant> page = ordered;
        if (afterId is { } cursor)
        {
            var index = ordered.FindIndex(t => t.Id.Value == cursor);
            page = index >= 0 ? ordered.Skip(index + 1) : ordered;
        }

        return page.Take(limitPlusOne).ToList();
    }

    public async Task AddAsync(LearnStackTenant tenant, CancellationToken cancellationToken) =>
        await db.Tenants.AddAsync(tenant, cancellationToken).ConfigureAwait(false);

    public async Task SaveChangesAsync(CancellationToken cancellationToken) =>
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
}
