using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LearnStack.Hub.Modules.Plans.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by <c>dotnet ef migrations</c>. The runtime context
/// is constructed against the shared scoped connection + unit of work; at design
/// time there is no DI, so this builds options from a plain connection string
/// (env-overridable, passwordless default — migrations add does not connect).
/// </summary>
public sealed class PlansDesignTimeDbContextFactory : IDesignTimeDbContextFactory<PlansDbContext>
{
    public PlansDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__HubDatabase")
            ?? "Host=localhost;Port=5432;Database=learnstack_hub;Username=learnstack";

        var options = new DbContextOptionsBuilder<PlansDbContext>()
            .UseNpgsql(connectionString, npg =>
                npg.MigrationsHistoryTable(PlansModuleRegistration.MigrationsHistoryTable, "hub"))
            .Options;

        return new PlansDbContext(options);
    }
}
