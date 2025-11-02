namespace Gringotts.Bot.Flows;

public sealed record TransferFlow(
    long? ToUserId,
    string? ToUsername,
    int? Amount,
    string? Note,
    string Step,                 // e.g. "PickRecipient" | "EnterAmount" | "Confirm"
    string IdempotencyKey) : IFlowState
{
    public FlowKind Kind => FlowKind.Transfer;
    public int Version => 1;     // bump when schema changes
}
