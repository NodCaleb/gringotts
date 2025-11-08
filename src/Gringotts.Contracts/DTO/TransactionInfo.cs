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

    // Returns a display string for this transaction from the perspective of the provided customerId.
    // - Arrow points left (⬅️) when the customer is the recipient (incoming), right (➡️) when the customer is the sender (outgoing).
    // - Shows counterpart name (sender or recipient), amount with +/-, and description.
    public string FormatForCustomer(long customerId)
    {
        var isRecipient = customerId == RecipientId;
        var isSender = SenderId.HasValue && customerId == SenderId.Value;

        // Arrow: incoming -> left, outgoing -> right, otherwise neutral
        var arrow = (isRecipient && Amount > 0) ? "⬅️" : "➡️";

        // Counterpart name: if customer is recipient, counterpart is sender (or employee); otherwise counterpart is recipient
        string counterpart = isRecipient
            ? (SenderName ?? EmployeeName ?? "System")
            : (RecipientName ?? "Unknown");

        // Amount sign: plus for incoming, minus for outgoing, blank otherwise
        var sign = (Amount < 0) ? string.Empty : isRecipient ? "+" : "-";
        var amountText = sign + Amount.ToString("N2");

        return $"{Date} {arrow} {counterpart}{Environment.NewLine}{amountText}{Environment.NewLine}{Description}{Environment.NewLine}";
    }
}
