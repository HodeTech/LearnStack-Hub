using LearnStack.Hub.Modules.Plans.Domain;

namespace LearnStack.Hub.Modules.Plans.Application.Abstractions;

/// <summary>
/// Persistence port for the <see cref="Plan"/> aggregate. Implemented over
/// <c>PlansDbContext</c> in the Infrastructure layer; returns domain
/// aggregates (no EF / IQueryable leak into Application).
/// </summary>
public interface IPlanRepository
{
    Task<Plan?> GetByIdAsync(PlanId id, CancellationToken cancellationToken);

    /// <summary>Keyset page (ordered by id); returns up to <paramref name="limit"/> + 1 to detect a next page.</summary>
    Task<IReadOnlyList<Plan>> ListAsync(
        bool? activeOnly,
        Guid? afterId,
        int limitPlusOne,
        CancellationToken cancellationToken);

    Task AddAsync(Plan plan, CancellationToken cancellationToken);

    /// <summary>Flushes tracked changes within the active unit-of-work transaction.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
