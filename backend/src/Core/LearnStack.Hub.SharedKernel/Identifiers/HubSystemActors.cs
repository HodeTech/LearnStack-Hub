namespace LearnStack.Hub.SharedKernel.Identifiers;

/// <summary>
/// Well-known synthetic actors used for system-initiated writes before a real
/// operator context exists. The resolved <c>OperatorContext</c> (from the
/// <c>learnstack-hub</c> Keycloak realm) lands in P02c-4; until then P02c-1
/// command handlers and the seeder stamp audit columns with
/// <see cref="SystemOperator"/> (a fixed, non-empty id so the
/// <c>AuditableEntity</c> audit-input guard is satisfied).
/// </summary>
public static class HubSystemActors
{
    /// <summary>The synthetic system operator for P02c-1 writes (replaced by the resolved operator in P02c-4).</summary>
    public static OperatorId SystemOperator { get; } =
        OperatorId.From(new Guid("00000000-0000-0000-0000-000000000001"));
}
