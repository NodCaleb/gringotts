using System.Data;
using Gringotts.Domain.Entities;

namespace Gringotts.Infrastructure.Interfaces;

public interface ITransactionsRepository
{
    Task<Transaction?> GetByIdAsync(Guid id, IDbConnection connection, IDbTransaction dbTransaction, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Transaction>> GetAllAsync(IDbConnection connection, IDbTransaction dbTransaction, CancellationToken cancellationToken = default);
    Task<Transaction> AddAsync(Transaction tx, IDbConnection connection, IDbTransaction dbTransaction, CancellationToken cancellationToken = default);
    Task UpdateAsync(Transaction tx, IDbConnection connection, IDbTransaction dbTransaction, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, IDbConnection connection, IDbTransaction dbTransaction, CancellationToken cancellationToken = default);
}
