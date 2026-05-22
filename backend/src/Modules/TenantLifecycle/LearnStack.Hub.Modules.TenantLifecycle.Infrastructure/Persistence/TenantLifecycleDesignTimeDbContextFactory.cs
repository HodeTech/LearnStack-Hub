using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LearnStack.Hub.Modules.TenantLifecycle.Infrastructure.Persistence;

/// <summary>Design-time factory for <c>dotnet ef migrations</c> (env-overridable, passwordless default).</summary>
public sealed class TenantLifecycleDesignTimeDbContextFactory : IDesignTimeDbContextFactory<TenantLifecycleDbContext>
{
    public TenantLifecycleDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__HubDatabase")
            ?? "Host=localhost;Port=5432;Database=learnstack_hub;Username=learnstack";

        var options = new DbContextOptionsBuilder<TenantLifecycleDbContext>()
            .UseNpgsql(connectionString, npg =>
                npg.MigrationsHistoryTable(TenantLifecycleModuleRegistration.MigrationsHistoryTable, "hub"))
            .Options;

        return new TenantLifecycleDbContext(options);
    }
}
