namespace LearnStack.Hub.SharedKernel.Compliance;

/// <summary>
/// A single compliance cap: whether a capability is <see cref="Allowed"/>,
/// whether it is <see cref="Forced"/> (operator-mandated, tenant cannot opt
/// out), and an optional <see cref="Value"/> (e.g. an audit-retention day count
/// or a data-residency region).
/// </summary>
/// <remarks>
/// The cross-cutting cap shape, shared by <c>Plan.compliance_defaults</c> and
/// the entitlement projection's <c>compliance.caps</c>, so the wire contract is
/// defined in one place. <strong>Empty in P02c-1</strong> — no plan compliance
/// defaults are wired and the projection's caps map is <c>{}</c>; the
/// <c>CompliancePolicy</c> module (P02c-5) populates it. <see cref="Value"/> is
/// modelled as a string for now; P02c-5 may refine it to a typed value when the
/// caps map is actually populated.
/// </remarks>
public sealed record ComplianceCap(bool Allowed, bool Forced, string? Value = null);
