using LearnStack.Hub.Modules.Subscriptions.Application.Abstractions;
using LearnStack.Hub.Modules.Subscriptions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace LearnStack.Hub.Modules.Subscriptions.Infrastructure;

/// <summary>Composition-root registration for the Subscriptions module.</summary>
public static class SubscriptionsModuleRegistration
{
    /// <summary>Per-module migrations history table (in the <c>hub</c> schema).</summary>
    public const string MigrationsHistoryTable = "__ef_migrations_history_subscriptions";

    public static IServiceCollection AddSubscriptionsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<SubscriptionsDbContext>((sp, options) =>
            options.UseNpgsql(
                sp.GetRequiredService<NpgsqlConnection>(),
                npg => npg.MigrationsHistoryTable(MigrationsHistoryTable, "hub")));

        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();

        return services;
    }
}
