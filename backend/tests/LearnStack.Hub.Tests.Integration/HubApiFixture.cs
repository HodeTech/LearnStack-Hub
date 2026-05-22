using LearnStack.Hub.Modules.Entitlements.Infrastructure;
using LearnStack.Hub.Modules.Entitlements.Infrastructure.Persistence;
using LearnStack.Hub.Modules.Plans.Infrastructure;
using LearnStack.Hub.Modules.Plans.Infrastructure.Persistence;
using LearnStack.Hub.Modules.Subscriptions.Infrastructure;
using LearnStack.Hub.Modules.Subscriptions.Infrastructure.Persistence;
using LearnStack.Hub.Modules.TenantLifecycle.Infrastructure;
using LearnStack.Hub.Modules.TenantLifecycle.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace LearnStack.Hub.Tests.Integration;

/// <summary>
/// Boots the Hub foundation host against a throwaway Postgres
/// (<c>learnstack_hub</c>) and applies every module's migrations into the
/// <c>hub</c> schema. Each command is sent through a fresh DI scope so it gets
/// its own shared-connection unit of work / transaction — mirroring the
/// per-request model.
/// </summary>
public sealed class HubApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:18.4-alpine")
        .WithDatabase("learnstack_hub")
        .Build();

    private WebApplicationFactory<Program>? _factory;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        var connectionString = _postgres.GetConnectionString();

        await MigrateAllAsync(connectionString);

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:HubDatabase", connectionString);
            builder.UseSetting("Hub:DeploymentMode", "Development");
        });

        // Touch the service provider so the host builds eagerly (surfaces wiring errors here).
        _ = _factory.Services;
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await _postgres.DisposeAsync();
    }

    /// <summary>Sends a request through a fresh DI scope (one transaction per top-level send).</summary>
    public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        await using var scope = _factory!.Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await mediator.Send(request, cancellationToken);
    }

    private static async Task MigrateAllAsync(string connectionString)
    {
        await MigrateAsync(
            new DbContextOptionsBuilder<TenantLifecycleDbContext>()
                .UseNpgsql(connectionString, npg => npg.MigrationsHistoryTable(
                    TenantLifecycleModuleRegistration.MigrationsHistoryTable, "hub")).Options,
            o => new TenantLifecycleDbContext(o));

        await MigrateAsync(
            new DbContextOptionsBuilder<PlansDbContext>()
                .UseNpgsql(connectionString, npg => npg.MigrationsHistoryTable(
                    PlansModuleRegistration.MigrationsHistoryTable, "hub")).Options,
            o => new PlansDbContext(o));

        await MigrateAsync(
            new DbContextOptionsBuilder<SubscriptionsDbContext>()
                .UseNpgsql(connectionString, npg => npg.MigrationsHistoryTable(
                    SubscriptionsModuleRegistration.MigrationsHistoryTable, "hub")).Options,
            o => new SubscriptionsDbContext(o));

        await MigrateAsync(
            new DbContextOptionsBuilder<EntitlementsDbContext>()
                .UseNpgsql(connectionString, npg => npg.MigrationsHistoryTable(
                    EntitlementsModuleRegistration.MigrationsHistoryTable, "hub")).Options,
            o => new EntitlementsDbContext(o));
    }

    private static async Task MigrateAsync<TContext>(DbContextOptions<TContext> options, Func<DbContextOptions<TContext>, TContext> factory)
        where TContext : DbContext
    {
        await using var context = factory(options);
        await context.Database.MigrateAsync();
    }
}

/// <summary>xUnit collection so the container + host are shared across the flow tests.</summary>
[CollectionDefinition(Name)]
public sealed class HubApiCollection : ICollectionFixture<HubApiFixture>
{
    public const string Name = "hub-api";
}
