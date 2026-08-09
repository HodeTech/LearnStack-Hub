using LearnStack.Hub.Modules.Entitlements.Application.Abstractions;
using LearnStack.Hub.Modules.Entitlements.Domain;
using LearnStack.Hub.SharedKernel.Identifiers;
using Microsoft.EntityFrameworkCore;

namespace LearnStack.Hub.Modules.Entitlements.Infrastructure.Persistence;

internal sealed class EntitlementRepository(EntitlementsDbContext db) : IEntitlementRepository
{
    public async Task<Entitlement?> GetByTenantAsync(LearnStackTenantId tenantId, CancellationToken cancellationToken) =>
        await db.Entitlements.FirstOrDefaultAsync(e => e.Id == tenantId, cancellationToken).ConfigureAwait(false);

    public async Task AddAsync(Entitlement entitlement, CancellationToken cancellationToken) =>
        await db.Entitlements.AddAsync(entitlement, cancellationToken).ConfigureAwait(false);

    public async Task SaveChangesAsync(CancellationToken cancellationToken) =>
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
}
