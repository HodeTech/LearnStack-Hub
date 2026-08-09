using LearnStack.Hub.Modules.TenantLifecycle.Application.Abstractions;
using LearnStack.Hub.Modules.TenantLifecycle.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace LearnStack.Hub.Modules.TenantLifecycle.Infrastructure;

/// <summary>Composition-root registration for the TenantLifecycle module.</summary>
public static class TenantLifecycleModuleRegistration
{
    /// <summary>Per-module migrations history table (in the <c>hub</c> schema).</summary>
    public const string MigrationsHistoryTable = "__ef_migrations_history_tenant_lifecycle";

    public static IServiceCollection AddTenantLifecycleModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<TenantLifecycleDbContext>((sp, options) =>
            options.UseNpgsql(
                sp.GetRequiredService<NpgsqlConnection>(),
                npg => npg.MigrationsHistoryTable(MigrationsHistoryTable, "hub")));

        services.AddScoped<ITenantRepository, TenantRepository>();

        return services;
    }
}
