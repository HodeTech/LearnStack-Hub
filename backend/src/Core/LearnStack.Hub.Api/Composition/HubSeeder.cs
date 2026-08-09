using LearnStack.Hub.Modules.Entitlements.Infrastructure.Persistence;
using LearnStack.Hub.Modules.Plans.Application.Contracts;
using LearnStack.Hub.Modules.Plans.Infrastructure.Persistence;
using LearnStack.Hub.Modules.Subscriptions.Infrastructure.Persistence;
using LearnStack.Hub.Modules.TenantLifecycle.Application.Contracts;
using LearnStack.Hub.Modules.TenantLifecycle.Infrastructure.Persistence;
using LearnStack.Hub.SharedKernel.FeatureFlags;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LearnStack.Hub.Api.Composition;

/// <summary>
/// Idempotent dev/demo seeder: applies every module's migrations, provisions the
/// four illustrative plan tiers (Architecture 24 § 8) as data, and creates a
/// demo tenant (+ trial subscription + entitlement) bound to Growth. Run via
/// <c>dotnet run -- --seed</c> (or <c>make seed</c>). Plans are data, never code.
/// </summary>
public static class HubSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        await using var scope = services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("HubSeeder");

        await MigrateAsync(sp, cancellationToken).ConfigureAwait(false);

        var mediator = sp.GetRequiredService<IMediator>();

        // Idempotent: only seed when the catalogue is empty.
        var existing = await mediator.Send(new ListPlansQuery(ActiveOnly: null, Cursor: null, Limit: 1), cancellationToken)
            .ConfigureAwait(false);
        if (existing.IsSuccess && existing.Value!.Items.Count > 0)
        {
            LogSkipped(logger, null);
            return;
        }

        var growthId = await SeedPlansAsync(mediator, cancellationToken).ConfigureAwait(false);

        var demo = await mediator.Send(
            new CreateTenantCommand("demo", "Demo Tenant", "SaaS", growthId),
            cancellationToken).ConfigureAwait(false);
        if (demo.IsSuccess)
        {
            LogComplete(logger, demo.Value!.Id, null);
        }
        else
        {
            LogDemoNotCreated(logger, demo.Error?.Code ?? "<unknown>", null);
        }
    }

    private static readonly Action<ILogger, Exception?> LogSkipped =
        LoggerMessage.Define(LogLevel.Information, new EventId(1, nameof(LogSkipped)),
            "Hub seed skipped — plans already present.");

    private static readonly Action<ILogger, Guid, Exception?> LogComplete =
        LoggerMessage.Define<Guid>(LogLevel.Information, new EventId(2, nameof(LogComplete)),
            "Hub seed complete — 4 plans + demo tenant {TenantId} provisioned.");

    private static readonly Action<ILogger, string, Exception?> LogDemoNotCreated =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(3, nameof(LogDemoNotCreated)),
            "Hub seed: demo tenant not created ({Code}).");

    private static async Task MigrateAsync(IServiceProvider sp, CancellationToken cancellationToken)
    {
        await sp.GetRequiredService<TenantLifecycleDbContext>().Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        await sp.GetRequiredService<PlansDbContext>().Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        await sp.GetRequiredService<SubscriptionsDbContext>().Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        await sp.GetRequiredService<EntitlementsDbContext>().Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<Guid> SeedPlansAsync(IMediator mediator, CancellationToken cancellationToken)
    {
        await Create(mediator, "Starter Monthly", "starter", 49m, StarterFeatures(), StarterLimits(), cancellationToken)
            .ConfigureAwait(false);

        // Growth uses the documented projection example values (entitlement-projection.md).
        var growth = await Create(mediator, "Growth Monthly", "growth", 199m, GrowthFeatures(), GrowthLimits(), cancellationToken)
            .ConfigureAwait(false);

        await Create(mediator, "Scale Monthly", "scale", 799m, ScaleFeatures(), ScaleLimits(), cancellationToken)
            .ConfigureAwait(false);

        await Create(mediator, "Enterprise", "enterprise", 0m, EnterpriseFeatures(), EnterpriseLimits(), cancellationToken)
            .ConfigureAwait(false);

        return growth;
    }

    private static async Task<Guid> Create(
        IMediator mediator,
        string name,
        string tier,
        decimal price,
        IReadOnlyDictionary<string, bool> features,
        IReadOnlyDictionary<string, long> limits,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreatePlanCommand(name, tier, features, limits, price, "monthly", "USD"),
            cancellationToken).ConfigureAwait(false);

        return result.IsSuccess
            ? result.Value!.Id
            : throw new InvalidOperationException($"Seed failed to create plan '{name}': {result.Error?.Code}");
    }

    private static Dictionary<string, bool> StarterFeatures() => Features(
        recording: false, customDomain: false, whiteLabel: false, unlimitedContent: false,
        ssoSaml: false, advancedReporting: false, apiAccess: false, webhooks: false, auditExport: false);

    private static Dictionary<string, long> StarterLimits() => Limits(
        maxUsers: 25, maxOrgs: 1, classroomMinutes: 0, recordingGb: 0, mediaGb: 5,
        bandwidthGb: 50, apiRate: 0, customContentTypes: 5, pageBlocks: 5);

    private static Dictionary<string, bool> GrowthFeatures() => Features(
        recording: true, customDomain: true, whiteLabel: true, unlimitedContent: true,
        ssoSaml: false, advancedReporting: false, apiAccess: true, webhooks: true, auditExport: true);

    private static Dictionary<string, long> GrowthLimits() => Limits(
        maxUsers: 500, maxOrgs: 10, classroomMinutes: 50_000, recordingGb: 500, mediaGb: 1000,
        bandwidthGb: 1000, apiRate: 6000, customContentTypes: -1, pageBlocks: -1);

    private static Dictionary<string, bool> ScaleFeatures() => Features(
        recording: true, customDomain: true, whiteLabel: true, unlimitedContent: true,
        ssoSaml: false, advancedReporting: true, apiAccess: true, webhooks: true, auditExport: true);

    private static Dictionary<string, long> ScaleLimits() => Limits(
        maxUsers: 5000, maxOrgs: 100, classroomMinutes: 500_000, recordingGb: 5000, mediaGb: 5000,
        bandwidthGb: 5000, apiRate: 60_000, customContentTypes: -1, pageBlocks: -1);

    private static Dictionary<string, bool> EnterpriseFeatures() => Features(
        recording: true, customDomain: true, whiteLabel: true, unlimitedContent: true,
        ssoSaml: true, advancedReporting: true, apiAccess: true, webhooks: true, auditExport: true);

    private static Dictionary<string, long> EnterpriseLimits() => Limits(
        maxUsers: -1, maxOrgs: -1, classroomMinutes: -1, recordingGb: -1, mediaGb: -1,
        bandwidthGb: -1, apiRate: -1, customContentTypes: -1, pageBlocks: -1);

    private static Dictionary<string, bool> Features(
        bool recording, bool customDomain, bool whiteLabel, bool unlimitedContent,
        bool ssoSaml, bool advancedReporting, bool apiAccess, bool webhooks, bool auditExport) =>
        new(StringComparer.Ordinal)
        {
            [FeatureKeys.ClassroomRecording.Value] = recording,
            [FeatureKeys.CustomDomain.Value] = customDomain,
            [FeatureKeys.WhiteLabelBranding.Value] = whiteLabel,
            [FeatureKeys.UnlimitedContentTypes.Value] = unlimitedContent,
            [FeatureKeys.SsoSaml.Value] = ssoSaml,
            [FeatureKeys.AdvancedReporting.Value] = advancedReporting,
            [FeatureKeys.ApiAccess.Value] = apiAccess,
            [FeatureKeys.Webhooks.Value] = webhooks,
            [FeatureKeys.AuditExport.Value] = auditExport,
        };

    private static Dictionary<string, long> Limits(
        long maxUsers, long maxOrgs, long classroomMinutes, long recordingGb, long mediaGb,
        long bandwidthGb, long apiRate, long customContentTypes, long pageBlocks) =>
        new(StringComparer.Ordinal)
        {
            [LimitKeys.MaxUsers.Value] = maxUsers,
            [LimitKeys.MaxOrganizations.Value] = maxOrgs,
            [LimitKeys.ClassroomMinutesPerMonth.Value] = classroomMinutes,
            [LimitKeys.RecordingStorageGb.Value] = recordingGb,
            [LimitKeys.MediaStorageGb.Value] = mediaGb,
            [LimitKeys.MediaBandwidthGbPerMonth.Value] = bandwidthGb,
            [LimitKeys.ApiRatePerMinute.Value] = apiRate,
            [LimitKeys.MaxCustomContentTypes.Value] = customContentTypes,
            [LimitKeys.MaxPageBlockDefinitions.Value] = pageBlocks,
        };
}
