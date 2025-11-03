using Gringotts.Contracts.Requests;
using Gringotts.Contracts.Results;
using Gringotts.Domain.Entities;

namespace Gringotts.Contracts.Interfaces;

public interface IApiClient
{
    Task<Result> CheckAccessCodeAsync(string userName, int accessCode, CancellationToken cancellationToken = default);

    Task<CustomerResult> GetCustomerByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<CustomerResult> CreateCustomerAsync(Customer customer, CancellationToken cancellationToken = default);

    Task<CustomerResult> UpdateCharacterNameAsync(long id, string characterName, CancellationToken cancellationToken = default);

    Task<TransactionResult> CreateTransactionAsync(TransactionRequest request, CancellationToken cancellationToken = default);

    Task<CustomersListResult> SearchCustomersAsync(string? search, int? pageNumber = null, int? pageSize = null, CancellationToken cancellationToken = default);
}
