using Gringotts.Contracts.DTO;

namespace Gringotts.Contracts.Responses;

public class TransactionResponse : BaseResponse
{
    public TransactionInfo? Transaction { get; set; }
}
