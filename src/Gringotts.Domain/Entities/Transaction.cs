namespace Gringotts.Domain.Entities;

public class Transaction
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; }
    public long Sender { get; set; }
    public long Recipient { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
}
