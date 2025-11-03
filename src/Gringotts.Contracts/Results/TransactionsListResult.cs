using Gringotts.Contracts.DTO;

namespace Gringotts.Contracts.Results;

public class TransactionsListResult : Result
{
    public List<TransactionInfo> Transactions { get; set; } = new();
}
