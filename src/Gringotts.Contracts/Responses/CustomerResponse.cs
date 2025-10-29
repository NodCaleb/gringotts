using Gringotts.Domain.Entities;

namespace Gringotts.Contracts.Responses;

public class CustomerResponse : BaseResponse
{
    public Customer? Customer { get; set; }
}
