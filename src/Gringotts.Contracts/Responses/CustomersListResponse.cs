using Gringotts.Contracts.Enums;
using Gringotts.Domain.Entities;

namespace Gringotts.Contracts.Responses;

public class CustomersListResponse
{
    public ErrorCode ErrorCode { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<Customer> Customers { get; set; } = new();
}
