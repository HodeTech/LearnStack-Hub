using LearnStack.Hub.Modules.Entitlements.Domain;
using LearnStack.Hub.SharedKernel.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LearnStack.Hub.Modules.Entitlements.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the Entitlements module. <c>hub</c> default schema,
/// jsonb columns for features / limits / compliance_caps, PK = tenant id, no
/// RLS. Enlists with the shared-connection unit of work.
/// </summary>
public sealed class EntitlementsDbContext : DbContext
{
    public EntitlementsDbContext(DbContextOptions<EntitlementsDbContext> options, IUnitOfWork? unitOfWork = null)
        : base(options)
    {
        unitOfWork?.Enlist(this);
    }

    public DbSet<Entitlement> Entitlements => Set<Entitlement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema("hub");
        modelBuilder.ApplyConfiguration(new EntitlementConfiguration());
    }
}
