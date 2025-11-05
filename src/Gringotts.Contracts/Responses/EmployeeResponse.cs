using Gringotts.Contracts.DTO;
using Gringotts.Shared.Enums;

namespace Gringotts.Contracts.Responses;

public class EmployeeResponse
{
    public ErrorCode ErrorCode { get; set; }
    public List<string> Errors { get; set; } = new();
    public EmployeeInfo? Employee { get; set; }
}
