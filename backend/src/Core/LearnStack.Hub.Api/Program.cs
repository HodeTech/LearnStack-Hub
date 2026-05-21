// TODO(2026-05-21, @platform, phase-02c-1): wire the cross-cutting foundation
// (mirror of LearnStack core P02a-3) — IExceptionHandler, 8-step MediatR
// pipeline, Result<T>.ToActionResult(), Serilog + OTLP, IErrorTrackingProvider,
// IProviderResilience<TPort>. The OpenTelemetry.* packages are reserved in
// Directory.Packages.props.
//
// TODO(2026-05-21, @platform, phase-02c-2): wire the four-endpoint Hub HTTPS
// contract surface — `POST /api/v1/internal/license/verify` and
// `POST /api/v1/usage/report` are HOSTED here; the outbound
// `POST /api/internal/tenants` + `PUT /api/internal/tenants/{id}/entitlements`
// calls live in LearnStack.Hub.Infrastructure.LearnStackApiClient.
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
