using Gringotts.Infrastructure.Interfaces;
using Npgsql;
using System.Data;

namespace Gringotts.Infrastructure.UnitOfWork;

internal class DapperUnitOfWork : IUnitOfWork
{
    private readonly NpgsqlConnection _connection;
    private IDbTransaction _transaction;
    private bool _disposed;

    public DapperUnitOfWork(string connectionString)
    {
        _connection = new NpgsqlConnection(connectionString);
        _connection.Open();
        _transaction = _connection.BeginTransaction();
    }

    public IDbConnection Connection => _connection;
    public IDbTransaction Transaction => _transaction;

    public async Task CommitAsync()
    {
        if (_transaction != null)
        {
            await ((NpgsqlTransaction)_transaction).CommitAsync();
            _transaction.Dispose();
            _transaction = null;
        }
    }

    public async Task RollbackAsync()
    {
        if (_transaction != null)
        {
            await ((NpgsqlTransaction)_transaction).RollbackAsync();
            _transaction.Dispose();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _transaction?.Dispose();
            _connection?.Dispose();
            _disposed = true;
        }
    }
}
