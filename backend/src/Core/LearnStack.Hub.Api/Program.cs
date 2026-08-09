using LearnStack.Hub.Api.Common;
using LearnStack.Hub.Api.Composition;
using LearnStack.Hub.Application.Pipeline;
using LearnStack.Hub.Infrastructure.Composition;
using LearnStack.Hub.Modules.Entitlements.Infrastructure;
using LearnStack.Hub.Modules.Plans.Infrastructure;
using LearnStack.Hub.Modules.Subscriptions.Infrastructure;
using LearnStack.Hub.Modules.TenantLifecycle.Infrastructure;
using LearnStack.Hub.SharedKernel.Hosting;

// TODO(P02c-2): wire the Hub HTTPS contract surface. It is governed by the two
// invariants in ADR-0034 — the Hub stores no tenant content, and every crossing
// goes through a named adapter — NOT by an endpoint count. Do not build "the four
// endpoints": ADR-0034 records that protecting that count is what caused TLS
// private keys to be tunnelled through the entitlement payload in the first place.
//
// HOSTED here (Hub receives): POST /api/v1/internal/license/verify,
// POST /api/v1/internal/license/refresh, POST /api/v1/usage/report, and
// POST /api/v1/internal/tenants/{id}/custom-domains.
// OUTBOUND (LearnStack.Hub.Infrastructure.LearnStackApiClient): POST /api/internal/tenants,
// PUT .../entitlements, PUT .../status, PUT .../host-mappings, DELETE /tenants/{id},
// GET .../usage. The host-mappings push is the endpoint that exists so certificate
// material stops riding the entitlement payload — do not omit it.
//
// Bind /api/internal/* to the internal listener only (Internal_API_Endpoints_AreNot_Public).
// Authoritative set:
// https://github.com/HodeTech/LearnStack/blob/main/docs/decisions/0034-hub-contract-surface-invariant.md

var builder = WebApplication.CreateBuilder(args);

// DeploymentMode is read exactly once, here at the composition root; modules
// never read it (Modules_Do_Not_Reference_DeploymentMode).
var deploymentMode = Enum.TryParse<DeploymentMode>(
    builder.Configuration["Hub:DeploymentMode"],
    ignoreCase: true,
    out var parsed)
    ? parsed
    : DeploymentMode.Development;

builder.AddHubSerilog();
builder.AddHubOpenTelemetry();

builder.Services.AddOpenApi();

// L1 exception handling → RFC 7807 Problem Details.
builder.Services.AddExceptionHandler<HubExceptionHandler>();
builder.Services.AddProblemDetails();

// Cross-cutting foundation: clock / guid / random / secrets, the
// DeploymentMode-branched IErrorTrackingProvider, and the shared-connection
// unit of work the live TransactionBehavior drives.
builder.Services.AddHubFoundation(builder.Configuration, deploymentMode);

// The 6-step MediatR pipeline + every module's handler assembly.
builder.Services.AddHubMediatRPipeline(
    typeof(LearnStack.Hub.Application.AssemblyMarker).Assembly,
    typeof(LearnStack.Hub.Modules.TenantLifecycle.Application.AssemblyMarker).Assembly,
    typeof(LearnStack.Hub.Modules.Plans.Application.AssemblyMarker).Assembly,
    typeof(LearnStack.Hub.Modules.Subscriptions.Application.AssemblyMarker).Assembly,
    typeof(LearnStack.Hub.Modules.Entitlements.Application.AssemblyMarker).Assembly);

// The four domain modules.
builder.Services.AddTenantLifecycleModule(builder.Configuration);
builder.Services.AddPlansModule(builder.Configuration);
builder.Services.AddSubscriptionsModule(builder.Configuration);
builder.Services.AddEntitlementsModule(builder.Configuration);

var app = builder.Build();

// `dotnet run -- --seed` (make seed): apply migrations + seed the plan catalogue
// and a demo tenant idempotently, then exit without starting the web host.
if (args.Contains("--seed", StringComparer.Ordinal))
{
    await LearnStack.Hub.Api.Composition.HubSeeder.SeedAsync(app.Services);
    return;
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/healthz", () => Results.Ok(new { status = "healthy", service = "learnstack-hub-api" }))
    .WithName("HealthCheck");

app.Run();

// `public partial class Program` exposes the entry-point type for
// WebApplicationFactory<Program> in the test assemblies and pins the
// LearnStack.Hub.Api assembly's IL TypeRef surface for NetArchTest scanning.
public partial class Program;
