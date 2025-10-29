namespace Gringotts.Infrastructure.Contracts;

public class TransactionRequest
{
    public long RecipientId { get; set; }
    public string? RecipientUsername { get; set; }
    public long? SenderId { get; set; }
    public Guid? EmployeeId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
}
