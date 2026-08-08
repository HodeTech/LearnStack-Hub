// TODO(2026-05-21, @platform, phase-02c-1): wire the cross-cutting foundation
// (adapted from LearnStack core P02a-3) — IExceptionHandler, the SIX-step
// MediatR pipeline, Result<T>.ToActionResult(), Serilog + OTLP,
// IErrorTrackingProvider, IProviderResilience<TPort>. The OpenTelemetry.*
// packages are reserved in Directory.Packages.props.
//
// The pipeline is six steps, not core's eight: Hub has no TenantContextBehavior
// (it stores no tenant content, so there is no tenant to resolve) and no
// OutboxFlushBehavior at this stage. See docs/architecture/cross-cutting-foundation.md.
//
// TODO(2026-05-21, @platform, phase-02c-2): wire the Hub HTTPS contract surface
// — `POST /api/v1/internal/license/verify`, `POST /api/v1/internal/license/refresh`
// and `POST /api/v1/usage/report` are HOSTED here; the outbound calls to
// LearnStack live in LearnStack.Hub.Infrastructure.LearnStackApiClient. The
// surface is governed by the two invariants in ADR-0034 (Hub stores no tenant
// content; every crossing goes through a named adapter), not by an endpoint
// count. See ../../../LearnStack/docs/decisions/0034-hub-contract-surface-invariant.md.
//
// TODO(2026-05-21, @platform, phase-02c-2): bind /api/internal/* endpoints
// only to the internal listener (separate Kestrel endpoint). Architecture
// test `Internal_API_Endpoints_AreNot_Public` will fail otherwise.

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/healthz", () => Results.Ok(new { status = "healthy", service = "learnstack-hub-api" }))
    .WithName("HealthCheck");

app.Run();

// `public partial class Program` serves two purposes:
//   1. Entry-point escape hatch: WebApplicationFactory<Program> in the test
//      assemblies resolves the entry-point type via this declaration.
//   2. Assembly marker: this is the intentional public type that pins the
//      LearnStack.Hub.Api assembly's IL TypeRef surface for NetArchTest
//      scanning. The other six core projects ship a separate `AssemblyMarker`
//      class because they have no entry point; the Api project does not need
//      a duplicate marker — `Program` already plays that role. Same posture
//      as LearnStack core's `LearnStack.Api/Program.cs`.
public partial class Program;
