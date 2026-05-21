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

// `public partial class Program` is the top-level-statements escape hatch
// that lets WebApplicationFactory<Program> in the test assemblies resolve
// the entry-point type.
public partial class Program;
