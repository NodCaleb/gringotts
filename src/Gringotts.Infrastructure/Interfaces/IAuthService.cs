using Gringotts.Contracts.Results;

namespace Gringotts.Infrastructure.Interfaces;

public interface IAuthService
{
    Task<Result> CheckAccessCode(string userName, int accessCode);

    // Return list of employee usernames (no access codes)
    Task<IReadOnlyList<string>> GetEmployeeNamesAsync();
}
