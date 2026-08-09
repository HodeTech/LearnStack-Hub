using LearnStack.Hub.Modules.Entitlements.Application;
using LearnStack.Hub.Modules.Entitlements.Application.Abstractions;
using LearnStack.Hub.Modules.Entitlements.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace LearnStack.Hub.Modules.Entitlements.Infrastructure;

/// <summary>Composition-root registration for the Entitlements module (DbContext, repository, projection service).</summary>
public static class EntitlementsModuleRegistration
{
    /// <summary>Per-module migrations history table (in the <c>hub</c> schema).</summary>
    public const string MigrationsHistoryTable = "__ef_migrations_history_entitlements";

    public static IServiceCollection AddEntitlementsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<EntitlementsDbContext>((sp, options) =>
            options.UseNpgsql(
                sp.GetRequiredService<NpgsqlConnection>(),
                npg => npg.MigrationsHistoryTable(MigrationsHistoryTable, "hub")));

        services.AddScoped<IEntitlementRepository, EntitlementRepository>();
        services.AddScoped<EntitlementProjectionService>();

        return services;
    }
}
