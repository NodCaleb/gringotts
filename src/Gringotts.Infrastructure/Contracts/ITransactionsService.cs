using Gringotts.Contracts.Requests;
using Gringotts.Contracts.Results;

namespace Gringotts.Infrastructure.Contracts;

public interface ITransactionsService
{
    Task<TransactionResult> CreateTransactionAsync(TransactionRequest request);
}
