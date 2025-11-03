using Gringotts.Domain.Entities;

namespace Gringotts.Contracts.Results;

public class CustomersListResult : Result
{
    public List<Customer> Customers { get; set; } = new ();
}
