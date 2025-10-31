using System.Text.Json;

namespace Gringotts.Infrastructure.Caching;

internal static class CacheJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        // add converters as needed
        WriteIndented = false
    };

    public static byte[] Serialize<T>(T value) =>
        JsonSerializer.SerializeToUtf8Bytes(value, Options);

    public static T? Deserialize<T>(byte[]? data) =>
        data is { Length: > 0 } ? JsonSerializer.Deserialize<T>(data, Options) : default;
}
