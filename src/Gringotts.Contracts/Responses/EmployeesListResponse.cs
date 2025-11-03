using Gringotts.Shared.Enums;

namespace Gringotts.Contracts.Responses;

public class EmployeesListResponse
{
    public ErrorCode ErrorCode { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> EmployeeNames { get; set; } = new();
}
