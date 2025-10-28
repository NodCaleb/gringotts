using Gringotts.Domain.Entities;

namespace Gringotts.Infrastructure.Contracts;

public class CustomerResult : Result
{
    public Customer? Customer { get; set; }
}
