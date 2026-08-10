using LearnStack.Hub.Infrastructure.ErrorTracking;
using LearnStack.Hub.Infrastructure.Persistence;
using LearnStack.Hub.SharedKernel.Hosting;
using LearnStack.Hub.SharedKernel.Identifiers;
using LearnStack.Hub.SharedKernel.Observability;
using LearnStack.Hub.SharedKernel.Persistence;
using LearnStack.Hub.SharedKernel.Random;
using LearnStack.Hub.SharedKernel.Secrets;
using LearnStack.Hub.SharedKernel.Time;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace LearnStack.Hub.Infrastructure.Composition;

/// <summary>
/// Composition-root extension that wires the Hub cross-cutting foundation:
/// clock / guid / random / secrets, the <c>DeploymentMode</c>-branched
/// <see cref="IErrorTrackingProvider"/>, and the shared-connection unit of work
/// every module DbContext rides. <c>DeploymentMode</c> is read exactly once
/// here — modules never read it.
/// </summary>
public static class HubFoundationRegistration
{
    public static IServiceCollection AddHubFoundation(
        this IServiceCollection services,
        IConfiguration configuration,
        DeploymentMode deploymentMode)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Deterministic-abstraction singletons.
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IGuidFactory, SystemGuidFactory>();
        services.AddSingleton<IRandom, SystemRandom>();
        services.AddSingleton<ISecretProvider, ConfigurationSecretProvider>();

        // Error tracking — branched once by DeploymentMode.
        services.AddSingleton<IErrorTrackingProvider>(sp => deploymentMode switch
        {
            DeploymentMode.Development => new NoOpErrorTracker(),
            DeploymentMode.SelfHostedAirGapped => new LocalFileErrorTracker(
                configuration["ErrorTracking:LocalFile:Path"] ?? "logs/hub-errors.ndjson",
                sp.GetRequiredService<ILogger<LocalFileErrorTracker>>()),
            _ => new SentryErrorTracker(
                sp.GetRequiredService<ISecretProvider>(),
                sp.GetRequiredService<ILogger<SentryErrorTracker>>()),
        });

        // Shared Npgsql connection + the unit of work the live TransactionBehavior drives.
        var connectionString = ResolveConnectionString(configuration);
        services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));
        services.AddScoped(sp => sp.GetRequiredService<NpgsqlDataSource>().CreateConnection());
        services.AddScoped<IUnitOfWork, NpgsqlUnitOfWork>();

        return services;
    }

    /// <summary>
    /// Resolves the <c>learnstack_hub</c> connection string. Precedence:
    /// the explicit <c>ConnectionStrings:HubDatabase</c> value (set by the
    /// integration-test harness and production config), otherwise assembled
    /// from the <c>POSTGRES_*</c> environment / config keys the dev <c>.env</c>
    /// supplies. No credential literal is embedded in source — the password
    /// rides on <c>POSTGRES_PASSWORD</c> at runtime (empty falls back to the
    /// passwordless local-trust shape).
    /// </summary>
    public static string ResolveConnectionString(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var explicitConnection = configuration["ConnectionStrings:HubDatabase"];
        if (!string.IsNullOrWhiteSpace(explicitConnection))
        {
            return explicitConnection;
        }

        var host = configuration["POSTGRES_HOST"] ?? "localhost";
        var port = configuration["POSTGRES_PORT"] ?? "5432";
        // Key matches .env.example and infra/compose/dev.yml, which provision
        // ${HUB_POSTGRES_DB}. Reading a different key worked only because both
        // sides defaulted to the same literal.
        var database = configuration["HUB_POSTGRES_DB"] ?? "learnstack_hub";
        var username = configuration["POSTGRES_USER"] ?? "learnstack";
        var password = configuration["POSTGRES_PASSWORD"];

        var passwordPart = string.IsNullOrEmpty(password)
            ? string.Empty
            : $"Password={password};";

        return $"Host={host};Port={port};Database={database};Username={username};{passwordPart}";
    }
}
