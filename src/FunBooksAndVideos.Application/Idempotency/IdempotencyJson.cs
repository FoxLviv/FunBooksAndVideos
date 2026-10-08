using System.Text.Json;
using System.Text.Json.Serialization;

namespace FunBooksAndVideos.Application.Idempotency;

/// <summary>Serializer settings for responses stored in idempotency records (camelCase, enums as names).</summary>
public static class IdempotencyJson
{
    /// <summary>Shared, read-only options; use them both to store and to replay a response.</summary>
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() },
        };
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }
}
