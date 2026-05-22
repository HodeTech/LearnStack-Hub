using LearnStack.Hub.Modules.Entitlements.Domain;
using LearnStack.Hub.SharedKernel.Identifiers;

namespace LearnStack.Hub.Modules.Entitlements.Application.Abstractions;

/// <summary>Persistence port for the <see cref="Entitlement"/> projection (1:1 with tenant; PK = tenant id).</summary>
public interface IEntitlementRepository
{
    Task<Entitlement?> GetByTenantAsync(LearnStackTenantId tenantId, CancellationToken cancellationToken);

    Task AddAsync(Entitlement entitlement, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
