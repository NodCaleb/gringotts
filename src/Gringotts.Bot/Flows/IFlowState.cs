using System.Text.Json;

namespace Gringotts.Bot.Flows;

public enum FlowKind { None = 0, SetName = 1, Transfer = 2 }

public interface IFlowState
{
    FlowKind Kind { get; }
    int Version { get; }
}