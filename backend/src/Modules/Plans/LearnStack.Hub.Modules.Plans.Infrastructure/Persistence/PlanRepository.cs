using LearnStack.Hub.Modules.Plans.Application.Abstractions;
using LearnStack.Hub.Modules.Plans.Domain;
using Microsoft.EntityFrameworkCore;

namespace LearnStack.Hub.Modules.Plans.Infrastructure.Persistence;

internal sealed class PlanRepository(PlansDbContext db) : IPlanRepository
{
    public async Task<Plan?> GetByIdAsync(PlanId id, CancellationToken cancellationToken) =>
        await db.Plans.FirstOrDefaultAsync(p => p.Id == id, cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<Plan>> ListAsync(
        bool? activeOnly,
        Guid? afterId,
        int limitPlusOne,
        CancellationToken cancellationToken)
    {
        var query = db.Plans.AsNoTracking();
        if (activeOnly == true)
        {
            query = query.Where(p => p.IsActive);
        }

        // P02c-1 keyset slice is done in memory — plan volume is tiny. A SQL
        // keyset (ORDER BY ... WHERE id > cursor) replaces this when volume warrants.
        var rows = await query.ToListAsync(cancellationToken).ConfigureAwait(false);
        var ordered = rows
            .OrderBy(p => p.CreatedAt)
            .ThenBy(p => p.Id.Value)
            .ToList();

        IEnumerable<Plan> page = ordered;
        if (afterId is { } cursor)
        {
            var index = ordered.FindIndex(p => p.Id.Value == cursor);
            page = index >= 0 ? ordered.Skip(index + 1) : ordered;
        }

        return page.Take(limitPlusOne).ToList();
    }

    public async Task AddAsync(Plan plan, CancellationToken cancellationToken) =>
        await db.Plans.AddAsync(plan, cancellationToken).ConfigureAwait(false);

    public async Task SaveChangesAsync(CancellationToken cancellationToken) =>
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
}
