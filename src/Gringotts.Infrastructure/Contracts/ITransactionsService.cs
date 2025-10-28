namespace Gringotts.Infrastructure.Contracts;

public interface ITransactionsService
{
    Task<TransactionResult> CreateTransactionAsync(TransactionRequest request);
}
