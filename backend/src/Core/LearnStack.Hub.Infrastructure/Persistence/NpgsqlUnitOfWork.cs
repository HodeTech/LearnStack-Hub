using LearnStack.Hub.SharedKernel.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LearnStack.Hub.Infrastructure.Persistence;

/// <summary>
/// Shared-connection <see cref="IUnitOfWork"/>. Every Hub module DbContext is
/// constructed against the same scoped <see cref="NpgsqlConnection"/> and
/// enlists here on construction; this unit of work owns the single
/// <see cref="NpgsqlTransaction"/> all enlisted contexts ride on, so a command
/// that writes across modules (e.g. tenant create → subscription → entitlement)
/// is atomic without a distributed transaction.
/// </summary>
/// <remarks>
/// Scoped lifetime: one instance per request / per DI scope. The shared
/// connection is disposed by the DI container at scope end.
/// </remarks>
public sealed class NpgsqlUnitOfWork(NpgsqlConnection connection) : IUnitOfWork, IAsyncDisposable
{
    private readonly NpgsqlConnection _connection = connection
        ?? throw new ArgumentNullException(nameof(connection));

    private readonly List<DbContext> _enlisted = [];
    private NpgsqlTransaction? _transaction;

    public bool HasActiveTransaction => _transaction is not null;

    public void Enlist(DbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!_enlisted.Contains(context))
        {
            _enlisted.Add(context);
        }

        // A context resolved after BeginAsync joins the active transaction now.
        if (_transaction is not null)
        {
            context.Database.UseTransaction(_transaction);
        }
    }

    public async Task BeginAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            return;
        }

        if (_connection.State != System.Data.ConnectionState.Open)
        {
            await _connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        _transaction = await _connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        // Contexts enlisted before the transaction opened (e.g. via the handler's
        // constructor) join it now.
        foreach (var context in _enlisted)
        {
            await context.Database.UseTransactionAsync(_transaction, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            return;
        }

        await _transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        await DisposeTransactionAsync().ConfigureAwait(false);
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            return;
        }

        await _transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
        await DisposeTransactionAsync().ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeTransactionAsync().ConfigureAwait(false);
    }

    private async Task DisposeTransactionAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.DisposeAsync().ConfigureAwait(false);
            _transaction = null;
        }
    }
}
