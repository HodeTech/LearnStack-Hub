using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LearnStack.Hub.Modules.Subscriptions.Infrastructure.Persistence;

/// <summary>Design-time factory for <c>dotnet ef migrations</c> (env-overridable, passwordless default).</summary>
public sealed class SubscriptionsDesignTimeDbContextFactory : IDesignTimeDbContextFactory<SubscriptionsDbContext>
{
    public SubscriptionsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__HubDatabase")
            ?? "Host=localhost;Port=5432;Database=learnstack_hub;Username=learnstack";

        var options = new DbContextOptionsBuilder<SubscriptionsDbContext>()
            .UseNpgsql(connectionString, npg =>
                npg.MigrationsHistoryTable(SubscriptionsModuleRegistration.MigrationsHistoryTable, "hub"))
            .Options;

        return new SubscriptionsDbContext(options);
    }
}
