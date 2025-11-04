using Gringotts.Shared.Enums;

namespace Gringotts.Contracts.Responses;

// Response for authentication check which includes EmployeeId when available
public class AuthResponse : BaseResponse
{
    public Guid? EmployeeId { get; set; }
}
