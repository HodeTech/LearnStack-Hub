namespace LearnStack.Hub.SharedKernel.Hosting;

/// <summary>
/// The deployment shape the composition root branches on (mirror of LearnStack
/// core per ADR-0020). Read exactly once at the composition root to select
/// provider implementations; modules <strong>never</strong> read this enum —
/// the architecture test <c>Modules_Do_Not_Reference_DeploymentMode</c>
/// enforces the rule.
/// </summary>
public enum DeploymentMode
{
    Development,
    SaaS,
    Dedicated,
    SelfHostedOnline,
    SelfHostedAirGapped,
}
