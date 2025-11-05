using Gringotts.Contracts.Requests;
using Gringotts.Contracts.Results;

namespace Gringotts.Contracts.Interfaces;

public interface IBffClient
{
    Task<CustomerResult> GetCustomerByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<TransactionResult> CreateTransactionAsync(TransactionRequest request, CancellationToken cancellationToken = default);

    Task<CustomersListResult> SearchCustomersAsync(string? search, int? pageNumber = null, int? pageSize = null, CancellationToken cancellationToken = default);

    Task<TransactionsListResult> GetTransactionsAsync(int? pageNumber = null, int? pageSize = null, CancellationToken cancellationToken = default);

    Task<TransactionsListResult> GetTransactionsByCustomerAsync(long customerId, int? pageNumber = null, int? pageSize = null, CancellationToken cancellationToken = default);

    Task<AuthResult> LoginAsync(AuthRequest request, CancellationToken cancellationToken = default);

    bool HasAuthCookie { get; }
}
