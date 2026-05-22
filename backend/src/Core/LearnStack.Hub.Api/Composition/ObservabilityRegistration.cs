using System.Globalization;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.OpenTelemetry;

namespace LearnStack.Hub.Api.Composition;

/// <summary>
/// Wires the Hub observability pipeline (cross-cutting-foundation.md § 4):
/// Serilog as the primary logger with a console sink + an OTLP sink, and the
/// OpenTelemetry SDK for tracing + metrics. The OTLP exporter is guarded behind
/// a configured endpoint so the host boots cleanly with no collector present
/// (dev, CI, contract tests). The OTel logging provider is intentionally NOT
/// registered alongside Serilog — Serilog's OTLP sink is the single log export
/// path (no double-export).
/// </summary>
internal static class ObservabilityRegistration
{
    private const string ServiceName = "learnstack-hub-api";
    private const string MediatRActivitySource = "learnstack.hub.mediatr";
    private const string ModuleActivitySourceWildcard = "learnstack.hub.*";

    public static void AddHubSerilog(this WebApplicationBuilder builder)
    {
        var otlpEndpoint = ResolveOtlpEndpoint(builder);

        builder.Host.UseSerilog((_, loggerConfiguration) =>
        {
            loggerConfiguration
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture);

            if (otlpEndpoint is not null)
            {
                loggerConfiguration.WriteTo.OpenTelemetry(options =>
                {
                    options.Endpoint = otlpEndpoint.ToString();
                    options.Protocol = OtlpProtocol.Grpc;
                    options.ResourceAttributes = new Dictionary<string, object>
                    {
                        ["service.name"] = ServiceName,
                    };
                });
            }
        });
    }

    public static void AddHubOpenTelemetry(this WebApplicationBuilder builder)
    {
        var otlpEndpoint = ResolveOtlpEndpoint(builder);

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(ServiceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(MediatRActivitySource)
                    .AddSource(ModuleActivitySourceWildcard)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                if (otlpEndpoint is not null)
                {
                    tracing.AddOtlpExporter(o => o.Endpoint = otlpEndpoint);
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                if (otlpEndpoint is not null)
                {
                    metrics.AddOtlpExporter(o => o.Endpoint = otlpEndpoint);
                }
            });
    }

    private static Uri? ResolveOtlpEndpoint(WebApplicationBuilder builder)
    {
        var raw = builder.Configuration["Otlp:Endpoint"]
            ?? builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];

        return Uri.TryCreate(raw, UriKind.Absolute, out var uri) ? uri : null;
    }
}
