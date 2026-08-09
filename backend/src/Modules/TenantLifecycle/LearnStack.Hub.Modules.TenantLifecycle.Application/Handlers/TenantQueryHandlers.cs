using LearnStack.Hub.Modules.TenantLifecycle.Application.Abstractions;
using LearnStack.Hub.Modules.TenantLifecycle.Application.Contracts;
using LearnStack.Hub.Modules.TenantLifecycle.Domain;
using LearnStack.Hub.SharedKernel.Identifiers;
using LearnStack.Hub.SharedKernel.Localization;
using LearnStack.Hub.SharedKernel.Pagination;
using LearnStack.Hub.SharedKernel.Results;
using MediatR;

namespace LearnStack.Hub.Modules.TenantLifecycle.Application.Handlers;

public sealed class GetTenantQueryHandler(ITenantRepository repository)
    : IRequestHandler<GetTenantQuery, Result<TenantDetailDto>>
{
    public async Task<Result<TenantDetailDto>> Handle(GetTenantQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tenant = await repository.GetByIdAsync(LearnStackTenantId.From(request.TenantId), cancellationToken)
            .ConfigureAwait(false);

        return tenant is null
            ? Result<TenantDetailDto>.Fail(new Error(new LocalizedMessage("lockey_not_found")))
            : Result<TenantDetailDto>.Ok(tenant.ToDetailDto());
    }
}

public sealed class ListTenantsQueryHandler(ITenantRepository repository)
    : IRequestHandler<ListTenantsQuery, Result<Page<TenantSummaryDto>>>
{
    public async Task<Result<Page<TenantSummaryDto>>> Handle(ListTenantsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        TenantStatus? status = null;
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!Enum.TryParse<TenantStatus>(request.Status, ignoreCase: true, out var parsed))
            {
                return Result<Page<TenantSummaryDto>>.Fail(new Error(new LocalizedMessage("lockey_validation_failed")));
            }

            status = parsed;
        }

        var limit = NormaliseLimit(request.Limit);
        var afterId = CursorCodec.Decode(request.Cursor);

        var rows = await repository.ListAsync(status, afterId, limit + 1, cancellationToken).ConfigureAwait(false);

        var hasNext = rows.Count > limit;
        var pageItems = rows.Take(limit).Select(t => t.ToSummaryDto()).ToArray();
        var nextCursor = hasNext && pageItems.Length > 0 ? CursorCodec.Encode(pageItems[^1].Id) : null;

        var page = new Page<TenantSummaryDto>(
            pageItems,
            new PageInfo(nextCursor, request.Cursor, hasNext, request.Cursor is not null));

        return Result<Page<TenantSummaryDto>>.Ok(page);
    }

    private static int NormaliseLimit(int requested)
    {
        if (requested <= 0)
        {
            return CursorPagination.DefaultLimit;
        }

        return requested > CursorPagination.MaxLimit ? CursorPagination.MaxLimit : requested;
    }
}
