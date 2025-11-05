using Gringotts.Contracts.DTO;
using Gringotts.Shared.Enums;

namespace Gringotts.Contracts.Results;

public class EmployeeResult
{
    public bool Success { get; set; }
    public ErrorCode ErrorCode { get; set; }
    public List<string> Errors { get; set; } = new();
    public EmployeeInfo? Employee { get; set; }
}
