using LearnStack.Hub.Modules.TenantLifecycle.Domain;
using LearnStack.Hub.SharedKernel.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LearnStack.Hub.Modules.TenantLifecycle.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the TenantLifecycle module. <c>hub</c> default schema,
/// snake_case, no RLS / no global query filter (Hub is operator-administered).
/// Enlists with the shared-connection unit of work.
/// </summary>
public sealed class TenantLifecycleDbContext : DbContext
{
    public TenantLifecycleDbContext(DbContextOptions<TenantLifecycleDbContext> options, IUnitOfWork? unitOfWork = null)
        : base(options)
    {
        unitOfWork?.Enlist(this);
    }

    public DbSet<LearnStackTenant> Tenants => Set<LearnStackTenant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema("hub");
        modelBuilder.ApplyConfiguration(new TenantConfiguration());
    }
}
