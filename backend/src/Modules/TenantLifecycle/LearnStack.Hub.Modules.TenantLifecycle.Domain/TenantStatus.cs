namespace LearnStack.Hub.Modules.TenantLifecycle.Domain;

/// <summary>
/// Tenant lifecycle status. Persisted as snake_case text with a
/// <c>ck_tenants_status</c> check constraint. Transitions:
/// Trial → Active → (Suspended ⇄ Active) → Archived → Terminated.
/// </summary>
public enum TenantStatus
{
    Trial,
    Active,
    Suspended,
    Archived,
    Terminated,
}
