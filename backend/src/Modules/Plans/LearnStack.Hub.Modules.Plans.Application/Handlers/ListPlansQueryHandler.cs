using LearnStack.Hub.Modules.Plans.Application.Abstractions;
using LearnStack.Hub.Modules.Plans.Application.Contracts;
using LearnStack.Hub.SharedKernel.Pagination;
using LearnStack.Hub.SharedKernel.Results;
using MediatR;

namespace LearnStack.Hub.Modules.Plans.Application.Handlers;

public sealed class ListPlansQueryHandler(IPlanRepository repository)
    : IRequestHandler<ListPlansQuery, Result<Page<PlanSummaryDto>>>
{
    public async Task<Result<Page<PlanSummaryDto>>> Handle(ListPlansQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var limit = NormaliseLimit(request.Limit);
        var afterId = CursorCodec.Decode(request.Cursor);

        var rows = await repository
            .ListAsync(request.ActiveOnly, afterId, limit + 1, cancellationToken)
            .ConfigureAwait(false);

        var hasNext = rows.Count > limit;
        var pageItems = rows.Take(limit).Select(p => p.ToSummaryDto()).ToArray();

        var nextCursor = hasNext && pageItems.Length > 0
            ? CursorCodec.Encode(pageItems[^1].Id)
            : null;

        var page = new Page<PlanSummaryDto>(
            pageItems,
            new PageInfo(nextCursor, request.Cursor, hasNext, request.Cursor is not null));

        return Result<Page<PlanSummaryDto>>.Ok(page);
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
