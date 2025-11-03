using Gringotts.Domain.Entities;

namespace Gringotts.Bot.Flows;

public sealed record TransferFlow(
    string Step,                 // e.g. "PickRecipient" | "EnterAmount" | "Confirm"
    Customer Sender,
    Customer? Recipient = null,
    IEnumerable<Customer>? Customers = null,
    decimal? Amount = null,
    string? Description = null) : IFlowState
{
    public FlowKind Kind => FlowKind.Transfer;
    public int Version => 1;     // bump when schema changes
}
