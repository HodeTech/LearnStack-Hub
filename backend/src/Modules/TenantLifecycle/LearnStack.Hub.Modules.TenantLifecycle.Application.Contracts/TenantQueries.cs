using LearnStack.Hub.SharedKernel.Pagination;
using LearnStack.Hub.SharedKernel.Results;
using MediatR;

namespace LearnStack.Hub.Modules.TenantLifecycle.Application.Contracts;

/// <summary>Reads a single tenant by id.</summary>
public sealed record GetTenantQuery(Guid TenantId) : IRequest<Result<TenantDetailDto>>;

/// <summary>Lists tenants (cursor-paginated), optionally filtered by status / deployment mode (wire strings).</summary>
public sealed record ListTenantsQuery(
    string? Status,
    string? DeploymentMode,
    string? Cursor,
    int Limit) : IRequest<Result<Page<TenantSummaryDto>>>;
