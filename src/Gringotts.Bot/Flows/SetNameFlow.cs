namespace Gringotts.Bot.Flows;

public sealed record SetNameFlow(
    string? Name = null) : IFlowState
{
    public FlowKind Kind => FlowKind.SetName;
    public int Version => 1;
}
