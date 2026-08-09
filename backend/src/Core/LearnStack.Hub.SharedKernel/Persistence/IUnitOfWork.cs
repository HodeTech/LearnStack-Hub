using Microsoft.EntityFrameworkCore;

namespace LearnStack.Hub.SharedKernel.Persistence;

/// <summary>
/// The shared-connection transaction seam the 6-step MediatR pipeline's live
/// <c>TransactionBehavior</c> commits / rolls back. Hub keeps one DbContext per
/// module (no global DbContext), but a single command — e.g.
/// <c>CreateTenantCommand</c> — legitimately writes across the TenantLifecycle,
/// Subscriptions, and Entitlements contexts. To make that atomic without a
/// distributed transaction, every module DbContext is constructed against the
/// <em>same</em> scoped <c>NpgsqlConnection</c> and enlists itself here; the
/// unit of work owns the one transaction all enlisted contexts ride on.
/// </summary>
/// <remarks>
/// Nested MediatR sends (e.g. the Subscriptions handler invoking the
/// Entitlements recompute command) join the outer transaction: the behavior
/// checks <see cref="HasActiveTransaction"/> and short-circuits to the inner
/// pipeline rather than opening a second transaction.
/// </remarks>
public interface IUnitOfWork
{
    /// <summary><c>true</c> once <see cref="BeginAsync"/> has opened a transaction not yet committed / rolled back.</summary>
    bool HasActiveTransaction { get; }

    /// <summary>
    /// Registers a module DbContext with the unit of work. If a transaction is
    /// already active the context is immediately associated with it; otherwise
    /// it is associated when <see cref="BeginAsync"/> runs. Idempotent per context.
    /// </summary>
    void Enlist(DbContext context);

    /// <summary>Opens the shared connection (if needed) and begins the transaction. No-op if already active.</summary>
    Task BeginAsync(CancellationToken cancellationToken = default);

    /// <summary>Commits the active transaction. No-op if none is active.</summary>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>Rolls back the active transaction. No-op if none is active.</summary>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
