using Polly;

namespace LearnStack.Hub.SharedKernel.Resilience;

/// <summary>
/// Carrier for the Polly v8 <see cref="ResiliencePipeline"/> that wraps a
/// provider adapter (<c>IPaymentProvider</c>, certificate provider, …). Every
/// such adapter receives one of these in its constructor and routes outbound
/// calls through <see cref="Pipeline"/>. No adapter consumes it until P02c-2 /
/// Phase 09b, but the surface ships now for parity.
/// </summary>
/// <typeparam name="TPort">The port interface the resilience policy is keyed to (DI discriminator only).</typeparam>
#pragma warning disable CA1040 // Avoid empty interfaces — the generic type parameter is the DI discriminator.
public interface IProviderResilience<TPort>
#pragma warning restore CA1040
    where TPort : class
{
    /// <summary>The pre-built Polly v8 pipeline; thread-safe and meant to be reused.</summary>
    ResiliencePipeline Pipeline { get; }

    /// <summary>The configuration section name (<c>"payment"</c>, …).</summary>
    string PortName { get; }
}
