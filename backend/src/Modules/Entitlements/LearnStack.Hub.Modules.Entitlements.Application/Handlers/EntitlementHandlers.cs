using LearnStack.Hub.Modules.Entitlements.Application.Abstractions;
using LearnStack.Hub.Modules.Entitlements.Application.Contracts;
using LearnStack.Hub.SharedKernel.Identifiers;
using LearnStack.Hub.SharedKernel.Localization;
using LearnStack.Hub.SharedKernel.Results;
using MediatR;

namespace LearnStack.Hub.Modules.Entitlements.Application.Handlers;

public sealed class RecomputeEntitlementCommandHandler(EntitlementProjectionService projectionService)
    : IRequestHandler<RecomputeEntitlementCommand, Result<EntitlementProjectionDto>>
{
    public Task<Result<EntitlementProjectionDto>> Handle(RecomputeEntitlementCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return projectionService.RecomputeAsync(request.TenantId, cancellationToken);
    }
}

public sealed class GetEntitlementQueryHandler(IEntitlementRepository repository)
    : IRequestHandler<GetEntitlementQuery, Result<EntitlementProjectionDto>>
{
    public async Task<Result<EntitlementProjectionDto>> Handle(GetEntitlementQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var entitlement = await repository.GetByTenantAsync(LearnStackTenantId.From(request.TenantId), cancellationToken)
            .ConfigureAwait(false);

        return entitlement is null
            ? Result<EntitlementProjectionDto>.Fail(new Error(new LocalizedMessage("lockey_not_found")))
            : Result<EntitlementProjectionDto>.Ok(entitlement.ToProjectionDto());
    }
}
