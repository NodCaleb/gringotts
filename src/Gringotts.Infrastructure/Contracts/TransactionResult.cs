using Gringotts.Domain.Entities;

namespace Gringotts.Infrastructure.Contracts;

public class TransactionResult : Result
{
    public Transaction Transaction { get; set; }
}
