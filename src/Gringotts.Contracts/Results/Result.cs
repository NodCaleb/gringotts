using Gringotts.Contracts.Enums;

namespace Gringotts.Contracts.Results;

public class Result
{
    public bool Success { get; set; }
    public List<string> ErrorMessage { get; set; } = new();
    public ErrorCode ErrorCode { get; set; }
}
