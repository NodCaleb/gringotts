namespace Gringotts.Contracts.Results;

public class AuthResult : Result
{
    public Guid? EmployeeId { get; set; }
    public string? AccessToken { get; set; }
}
