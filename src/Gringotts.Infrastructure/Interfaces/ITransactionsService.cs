using Gringotts.Contracts.Requests;
using Gringotts.Contracts.Results;

namespace Gringotts.Infrastructure.Interfaces;

public interface ITransactionsService
{
    Task<TransactionResult> CreateTransactionAsync(TransactionRequest request);
}
