using Gringotts.Domain.Entities;

namespace Gringotts.Contracts.Results;

public class CustomerResult : Result
{
    public Customer? Customer { get; set; }
}
