using Gringotts.Domain.Entities;

namespace Gringotts.Contracts.Results;

public class SearchCustomerResult : Result
{
    public List<Customer> Customers { get; set; } = new ();
}
