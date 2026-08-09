using LearnStack.Hub.Modules.Plans.Domain;
using LearnStack.Hub.SharedKernel.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LearnStack.Hub.Modules.Plans.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the Plans module. <c>hub</c> default schema, snake_case
/// naming, no RLS / no global query filter (plans are global, operator-authored).
/// Enlists with the shared-connection unit of work so it rides the one
/// transaction the live TransactionBehavior owns.
/// </summary>
public sealed class PlansDbContext : DbContext
{
    public PlansDbContext(DbContextOptions<PlansDbContext> options, IUnitOfWork? unitOfWork = null)
        : base(options)
    {
        unitOfWork?.Enlist(this);
    }

    public DbSet<Plan> Plans => Set<Plan>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema("hub");
        modelBuilder.ApplyConfiguration(new PlanConfiguration());
    }
}
