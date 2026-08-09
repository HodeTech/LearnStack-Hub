using LearnStack.Hub.Modules.TenantLifecycle.Domain;
using LearnStack.Hub.SharedKernel.Identifiers;

namespace LearnStack.Hub.Modules.TenantLifecycle.Application.Abstractions;

/// <summary>Persistence port for the <see cref="LearnStackTenant"/> aggregate.</summary>
public interface ITenantRepository
{
    Task<LearnStackTenant?> GetByIdAsync(LearnStackTenantId id, CancellationToken cancellationToken);

    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken);

    Task<IReadOnlyList<LearnStackTenant>> ListAsync(
        TenantStatus? status,
        Guid? afterId,
        int limitPlusOne,
        CancellationToken cancellationToken);

    Task AddAsync(LearnStackTenant tenant, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
