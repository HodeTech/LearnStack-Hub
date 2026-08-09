using LearnStack.Hub.Modules.Plans.Application.Abstractions;
using LearnStack.Hub.Modules.Plans.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace LearnStack.Hub.Modules.Plans.Infrastructure;

/// <summary>
/// Composition-root registration for the Plans module: its DbContext (against
/// the shared connection so it joins the unit-of-work transaction) and its
/// repository. MediatR handlers + validators are picked up by the host's
/// assembly scan of <c>Application.AssemblyMarker</c>.
/// </summary>
public static class PlansModuleRegistration
{
    /// <summary>Per-module migrations history table (in the <c>hub</c> schema) so the four contexts coexist.</summary>
    public const string MigrationsHistoryTable = "__ef_migrations_history_plans";

    public static IServiceCollection AddPlansModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<PlansDbContext>((sp, options) =>
            options.UseNpgsql(
                sp.GetRequiredService<NpgsqlConnection>(),
                npg => npg.MigrationsHistoryTable(MigrationsHistoryTable, "hub")));

        services.AddScoped<IPlanRepository, PlanRepository>();

        return services;
    }
}
