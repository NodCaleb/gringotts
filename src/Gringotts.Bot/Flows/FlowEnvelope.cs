using System.Text.Json;

namespace Gringotts.Bot.Flows;

public sealed record FlowEnvelope(
    FlowKind Kind,
    int Version,
    JsonElement Payload);

public static class FlowEnvelopeHelper
{
    static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static FlowEnvelope Wrap(IFlowState state)
        => state switch
        {
            SetNameFlow s => new FlowEnvelope(s.Kind, s.Version, JsonSerializer.SerializeToElement(s, JsonOpts)),
            TransferFlow t => new FlowEnvelope(t.Kind, t.Version, JsonSerializer.SerializeToElement(t, JsonOpts)),
            _ => throw new NotSupportedException("Unknown flow state")
        };

    public static IFlowState Unwrap(FlowEnvelope env)
        => env.Kind switch
        {
            FlowKind.SetName => JsonSerializer.Deserialize<SetNameFlow>(env.Payload, JsonOpts)!,
            FlowKind.Transfer => JsonSerializer.Deserialize<TransferFlow>(env.Payload, JsonOpts)!,
            _ => throw new NotSupportedException($"Unknown kind {env.Kind}")
        };
}