using LearnStack.Hub.Api.Common;
using LearnStack.Hub.Api.Composition;
using LearnStack.Hub.Application.Pipeline;
using LearnStack.Hub.Infrastructure.Composition;
using LearnStack.Hub.Modules.Plans.Infrastructure;
using LearnStack.Hub.Modules.TenantLifecycle.Infrastructure;
using LearnStack.Hub.SharedKernel.Hosting;

// TODO(P02c-2): wire the four-endpoint Hub HTTPS contract surface —
// POST /api/v1/internal/license/verify and POST /api/v1/usage/report are HOSTED
// here; the outbound POST /api/internal/tenants + PUT .../entitlements calls
// live in LearnStack.Hub.Infrastructure.LearnStackApiClient. Bind /api/internal/*
// to the internal listener only (Internal_API_Endpoints_AreNot_Public).

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

// The 6-step MediatR pipeline + module handler scanning. The Subscriptions +
// Entitlements module assemblies are appended in P02c-1d.
builder.Services.AddHubMediatRPipeline(
    typeof(LearnStack.Hub.Application.AssemblyMarker).Assembly,
    typeof(LearnStack.Hub.Modules.TenantLifecycle.Application.AssemblyMarker).Assembly,
    typeof(LearnStack.Hub.Modules.Plans.Application.AssemblyMarker).Assembly);

// Domain modules. Subscriptions + Entitlements are registered in P02c-1d.
builder.Services.AddTenantLifecycleModule(builder.Configuration);
builder.Services.AddPlansModule(builder.Configuration);

var app = builder.Build();

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
