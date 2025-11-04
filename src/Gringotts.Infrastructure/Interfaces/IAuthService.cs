using Gringotts.Contracts.DTO;
using Gringotts.Contracts.Results;

namespace Gringotts.Infrastructure.Interfaces;

public interface IAuthService
{
    Task<AuthResult> CheckAccessCode(string userName, int accessCode);

    // Return list of employee info (id + username), access codes filtered out
    Task<IReadOnlyList<EmployeeInfo>> GetEmployeeListAsync();
}
