using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LearnStack.Hub.Modules.Entitlements.Infrastructure.Persistence;

/// <summary>Design-time factory for <c>dotnet ef migrations</c> (env-overridable, passwordless default).</summary>
public sealed class EntitlementsDesignTimeDbContextFactory : IDesignTimeDbContextFactory<EntitlementsDbContext>
{
    public EntitlementsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__HubDatabase")
            ?? "Host=localhost;Port=5432;Database=learnstack_hub;Username=learnstack";

        var options = new DbContextOptionsBuilder<EntitlementsDbContext>()
            .UseNpgsql(connectionString, npg =>
                npg.MigrationsHistoryTable(EntitlementsModuleRegistration.MigrationsHistoryTable, "hub"))
            .Options;

        return new EntitlementsDbContext(options);
    }
}
