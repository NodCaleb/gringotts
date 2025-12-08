using Gringotts.Contracts.Enums;

namespace Gringotts.Contracts.Responses;

public class BaseResponse
{
    public ErrorCode ErrorCode { get; set; }
    public List<string> Errors { get; set; } = new();
}
