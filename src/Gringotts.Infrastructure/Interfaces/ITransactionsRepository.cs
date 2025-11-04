using System.Data;
using Gringotts.Domain.Entities;
using Gringotts.Contracts.DTO;

namespace Gringotts.Infrastructure.Interfaces;

public interface ITransactionsRepository
{
    Task<TransactionInfo?> GetByIdAsync(Guid id, IDbConnection connection, IDbTransaction dbTransaction, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TransactionInfo>> GetAllAsync(IDbConnection connection, IDbTransaction dbTransaction, CancellationToken cancellationToken = default);
    Task<Transaction> AddAsync(Transaction tx, IDbConnection connection, IDbTransaction dbTransaction, CancellationToken cancellationToken = default);
    Task UpdateAsync(Transaction tx, IDbConnection connection, IDbTransaction dbTransaction, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, IDbConnection connection, IDbTransaction dbTransaction, CancellationToken cancellationToken = default);

    // Returns transactions where the specified customer is either sender or recipient.
    // Results are sorted by date desc. Optional pagination: pageNumber (1-based) and pageSize.
    Task<IReadOnlyList<TransactionInfo>> GetByCustomerAsync(long customerId, IDbConnection connection, IDbTransaction dbTransaction, int? pageNumber = null, int? pageSize = null, CancellationToken cancellationToken = default);
}
