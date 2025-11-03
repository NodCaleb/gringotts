namespace Gringotts.Contracts.DTO;

public class TransactionInfo
{
    public Guid Id { get; set; }
    public long RecipientId { get; set; }
    public string RecipientName { get; set; } = string.Empty;
    public long? SenderId { get; set; }
    public string? SenderName { get; set; }
    public Guid? EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
}
