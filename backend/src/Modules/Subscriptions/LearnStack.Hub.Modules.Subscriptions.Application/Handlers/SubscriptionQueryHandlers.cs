using LearnStack.Hub.Modules.Subscriptions.Application.Abstractions;
using LearnStack.Hub.Modules.Subscriptions.Application.Contracts;
using LearnStack.Hub.Modules.Subscriptions.Domain;
using LearnStack.Hub.SharedKernel.Identifiers;
using LearnStack.Hub.SharedKernel.Localization;
using LearnStack.Hub.SharedKernel.Pagination;
using LearnStack.Hub.SharedKernel.Results;
using MediatR;

namespace LearnStack.Hub.Modules.Subscriptions.Application.Handlers;

public sealed class GetSubscriptionQueryHandler(ISubscriptionRepository repository)
    : IRequestHandler<GetSubscriptionQuery, Result<SubscriptionDto>>
{
    public async Task<Result<SubscriptionDto>> Handle(GetSubscriptionQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var subscription = await repository.GetByTenantAsync(LearnStackTenantId.From(request.TenantId), cancellationToken)
            .ConfigureAwait(false);

        return subscription is null
            ? Result<SubscriptionDto>.Fail(new Error(new LocalizedMessage("lockey_not_found")))
            : Result<SubscriptionDto>.Ok(subscription.ToDto());
    }
}

public sealed class GetSubscriptionsByPlanQueryHandler(ISubscriptionRepository repository)
    : IRequestHandler<GetSubscriptionsByPlanQuery, Result<IReadOnlyList<Guid>>>
{
    public async Task<Result<IReadOnlyList<Guid>>> Handle(GetSubscriptionsByPlanQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tenantIds = await repository.GetTenantIdsByPlanAsync(request.PlanId, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<Guid> ids = tenantIds.Select(id => id.Value).ToArray();
        return Result<IReadOnlyList<Guid>>.Ok(ids);
    }
}

public sealed class ListSubscriptionsQueryHandler(ISubscriptionRepository repository)
    : IRequestHandler<ListSubscriptionsQuery, Result<Page<SubscriptionSummaryDto>>>
{
    public async Task<Result<Page<SubscriptionSummaryDto>>> Handle(ListSubscriptionsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        SubscriptionStatus? status = null;
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!Enum.TryParse<SubscriptionStatus>(request.Status, ignoreCase: true, out var parsed))
            {
                return Result<Page<SubscriptionSummaryDto>>.Fail(new Error(new LocalizedMessage("lockey_validation_failed")));
            }

            status = parsed;
        }

        var limit = NormaliseLimit(request.Limit);
        var afterId = CursorCodec.Decode(request.Cursor);

        var rows = await repository.ListAsync(status, afterId, limit + 1, cancellationToken).ConfigureAwait(false);

        var hasNext = rows.Count > limit;
        var pageItems = rows.Take(limit).Select(s => s.ToSummaryDto()).ToArray();
        var nextCursor = hasNext && pageItems.Length > 0 ? CursorCodec.Encode(pageItems[^1].TenantId) : null;

        var page = new Page<SubscriptionSummaryDto>(
            pageItems,
            new PageInfo(nextCursor, request.Cursor, hasNext, request.Cursor is not null));

        return Result<Page<SubscriptionSummaryDto>>.Ok(page);
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
