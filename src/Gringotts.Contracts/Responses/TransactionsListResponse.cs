using Gringotts.Contracts.DTO;
using Gringotts.Shared.Enums;

namespace Gringotts.Contracts.Responses;

public class TransactionsListResponse
{
    public ErrorCode ErrorCode { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<TransactionInfo> Transactions { get; set; } = new();
}
