namespace Gringotts.Infrastructure.Contracts;

public class Result
{
    public bool Success { get; set; }
    public List<string> ErrorMessage { get; set; } = new();
}
