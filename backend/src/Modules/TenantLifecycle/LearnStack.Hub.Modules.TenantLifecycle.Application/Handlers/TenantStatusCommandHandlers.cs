using LearnStack.Hub.Modules.TenantLifecycle.Application.Abstractions;
using LearnStack.Hub.Modules.TenantLifecycle.Application.Contracts;
using LearnStack.Hub.SharedKernel.Identifiers;
using LearnStack.Hub.SharedKernel.Localization;
using LearnStack.Hub.SharedKernel.Results;
using LearnStack.Hub.SharedKernel.Time;
using MediatR;

namespace LearnStack.Hub.Modules.TenantLifecycle.Application.Handlers;

// The entitlement projection carries no tenant-status field (tier/features/
// limits derive from Plan + Subscription), so tenant status transitions do not
// trigger an entitlement recompute — only subscription / plan changes do, per
// the recompute-trigger table in entitlement-projection.md.

public sealed class ActivateTenantCommandHandler(ITenantRepository repository, IClock clock)
    : IRequestHandler<ActivateTenantCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(ActivateTenantCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tenant = await repository.GetByIdAsync(LearnStackTenantId.From(request.TenantId), cancellationToken)
            .ConfigureAwait(false);
        if (tenant is null)
        {
            return Result<Unit>.Fail(new Error(new LocalizedMessage("lockey_not_found")));
        }

        var result = tenant.Activate(clock, HubSystemActors.SystemOperator);
        if (result.IsFailure)
        {
            return result;
        }

        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<Unit>.Ok(Unit.Value);
    }
}

public sealed class SuspendTenantCommandHandler(ITenantRepository repository, IClock clock)
    : IRequestHandler<SuspendTenantCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(SuspendTenantCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tenant = await repository.GetByIdAsync(LearnStackTenantId.From(request.TenantId), cancellationToken)
            .ConfigureAwait(false);
        if (tenant is null)
        {
            return Result<Unit>.Fail(new Error(new LocalizedMessage("lockey_not_found")));
        }

        var result = tenant.Suspend(request.Reason, clock, HubSystemActors.SystemOperator);
        if (result.IsFailure)
        {
            return result;
        }

        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<Unit>.Ok(Unit.Value);
    }
}

public sealed class ArchiveTenantCommandHandler(ITenantRepository repository, IClock clock)
    : IRequestHandler<ArchiveTenantCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(ArchiveTenantCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tenant = await repository.GetByIdAsync(LearnStackTenantId.From(request.TenantId), cancellationToken)
            .ConfigureAwait(false);
        if (tenant is null)
        {
            return Result<Unit>.Fail(new Error(new LocalizedMessage("lockey_not_found")));
        }

        var result = tenant.Archive(clock, HubSystemActors.SystemOperator);
        if (result.IsFailure)
        {
            return result;
        }

        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<Unit>.Ok(Unit.Value);
    }
}
