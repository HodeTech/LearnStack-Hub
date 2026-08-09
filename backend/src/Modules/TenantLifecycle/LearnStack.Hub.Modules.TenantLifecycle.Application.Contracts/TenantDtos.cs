namespace LearnStack.Hub.Modules.TenantLifecycle.Application.Contracts;

/// <summary>Returned by <see cref="CreateTenantCommand"/> once the tenant (+ trial subscription + entitlement) is provisioned.</summary>
public sealed record TenantCreatedDto(Guid Id, string Slug, string Status);

/// <summary>Full tenant projection for the detail view.</summary>
public sealed record TenantDetailDto(
    Guid Id,
    string Slug,
    string DisplayName,
    string Status,
    string DeploymentMode,
    DateTimeOffset? LastPhoneHomeAt);

/// <summary>Compact tenant projection for list endpoints.</summary>
public sealed record TenantSummaryDto(
    Guid Id,
    string Slug,
    string DisplayName,
    string Status,
    string DeploymentMode);
