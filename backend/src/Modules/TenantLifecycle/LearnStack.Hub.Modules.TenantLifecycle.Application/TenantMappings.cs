using LearnStack.Hub.Modules.TenantLifecycle.Application.Contracts;
using LearnStack.Hub.Modules.TenantLifecycle.Domain;

namespace LearnStack.Hub.Modules.TenantLifecycle.Application;

/// <summary>Maps the <see cref="LearnStackTenant"/> aggregate to its contract DTOs.</summary>
internal static class TenantMappings
{
    public static TenantDetailDto ToDetailDto(this LearnStackTenant tenant) => new(
        tenant.Id.Value,
        tenant.Slug,
        tenant.DisplayName,
        tenant.Status.ToString(),
        tenant.DeploymentMode.ToString(),
        tenant.LastPhoneHomeAt);

    public static TenantSummaryDto ToSummaryDto(this LearnStackTenant tenant) => new(
        tenant.Id.Value,
        tenant.Slug,
        tenant.DisplayName,
        tenant.Status.ToString(),
        tenant.DeploymentMode.ToString());
}
