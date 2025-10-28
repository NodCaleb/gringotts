namespace Gringotts.Infrastructure.Contracts;

public interface IAuthService
{
    Task<Result> CheckAccessCode(string userName, int accessCode);
}
