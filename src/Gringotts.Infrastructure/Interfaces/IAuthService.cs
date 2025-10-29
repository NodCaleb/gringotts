using Gringotts.Contracts.Results;

namespace Gringotts.Infrastructure.Interfaces;

public interface IAuthService
{
    Task<Result> CheckAccessCode(string userName, int accessCode);
}
