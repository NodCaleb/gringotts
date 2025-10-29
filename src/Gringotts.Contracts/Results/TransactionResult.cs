using Gringotts.Domain.Entities;

namespace Gringotts.Contracts.Results;

public class TransactionResult : Result
{
    public Transaction Transaction { get; set; }
}
