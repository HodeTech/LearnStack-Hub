using LearnStack.Hub.SharedKernel.Resilience;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace LearnStack.Hub.Infrastructure.Resilience;

/// <summary>
/// Polly v8 implementation of <see cref="IProviderResilience{TPort}"/>. Builds
/// the <see cref="ResiliencePipeline"/> once from the supplied
/// <see cref="ResilienceOptions"/> (retry → circuit breaker → timeout, optional
/// bulkhead). No adapter consumes it in P02c-1 — the surface ships so P02c-2 /
/// Phase 09b adapters can take an instance in their constructor.
/// </summary>
public sealed class PollyProviderResilience<TPort> : IProviderResilience<TPort>
    where TPort : class
{
    public PollyProviderResilience(string portName, ResilienceOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(portName);
        ArgumentNullException.ThrowIfNull(options);

        PortName = portName;
        Pipeline = Build(options);
    }

    public ResiliencePipeline Pipeline { get; }

    public string PortName { get; }

    private static ResiliencePipeline Build(ResilienceOptions options)
    {
        var builder = new ResiliencePipelineBuilder();

        if (options.Retry.Enabled)
        {
            builder.AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = options.Retry.MaxAttempts,
                Delay = TimeSpan.FromSeconds(options.Retry.DelaySeconds),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = options.Retry.UseJitter,
            });
        }

        if (options.CircuitBreaker.Enabled)
        {
            builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = options.CircuitBreaker.FailureRatio,
                SamplingDuration = TimeSpan.FromSeconds(options.CircuitBreaker.SamplingDurationSeconds),
                MinimumThroughput = options.CircuitBreaker.MinimumThroughput,
                BreakDuration = TimeSpan.FromSeconds(options.CircuitBreaker.BreakDurationSeconds),
            });
        }

        if (options.Timeout.Enabled)
        {
            builder.AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(options.Timeout.TotalSeconds),
            });
        }

        return builder.Build();
    }
}
