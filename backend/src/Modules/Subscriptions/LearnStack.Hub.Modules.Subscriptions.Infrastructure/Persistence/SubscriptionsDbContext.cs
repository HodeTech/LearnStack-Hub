using LearnStack.Hub.Modules.Subscriptions.Domain;
using LearnStack.Hub.SharedKernel.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LearnStack.Hub.Modules.Subscriptions.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the Subscriptions module. <c>hub</c> default schema, no
/// RLS / no global query filter. Cross-module FKs (tenant_id, plan_id) are plain
/// uuid columns + indexes, not EF navigations into other modules' entities.
/// Enlists with the shared-connection unit of work.
/// </summary>
public sealed class SubscriptionsDbContext : DbContext
{
    public SubscriptionsDbContext(DbContextOptions<SubscriptionsDbContext> options, IUnitOfWork? unitOfWork = null)
        : base(options)
    {
        unitOfWork?.Enlist(this);
    }

    public DbSet<HubSubscription> Subscriptions => Set<HubSubscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema("hub");
        modelBuilder.ApplyConfiguration(new SubscriptionConfiguration());
    }
}
