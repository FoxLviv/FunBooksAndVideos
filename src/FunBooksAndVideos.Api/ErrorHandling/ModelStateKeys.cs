using System.Text.Json;

namespace FunBooksAndVideos.Api.ErrorHandling;

/// <summary>
/// Maps MVC model-state keys (C# member paths such as <c>Lines[0].ProductId</c>) onto the JSON contract
/// (<c>lines[0].productId</c>), so 400 and 422 responses key their per-field errors the same way.
/// </summary>
internal static class ModelStateKeys
{
    /// <summary>
    /// Converts a model-state key to its JSON form. Each <c>.</c>-separated segment has the identifier before any
    /// <c>[</c> camel-cased. Keys that are already JSON paths (starting with <c>$</c>), header names (containing
    /// <c>-</c>) and empty keys are returned unchanged.
    /// </summary>
    /// <param name="key">The model-state key.</param>
    /// <returns>The key as it appears in the JSON contract.</returns>
    public static string ToJsonKey(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (key.Length == 0 || key.StartsWith('$') || key.Contains('-', StringComparison.Ordinal))
        {
            return key;
        }

        var segments = key.Split('.');
        for (var i = 0; i < segments.Length; i++)
        {
            segments[i] = ToJsonSegment(segments[i]);
        }

        return string.Join('.', segments);
    }

    /// <summary>
    /// Re-keys an error dictionary with <see cref="ToJsonKey"/>, merging the messages of keys that collapse
    /// onto the same JSON key while keeping their order.
    /// </summary>
    /// <param name="errors">The per-field errors keyed by model-state key.</param>
    /// <returns>A new dictionary keyed by JSON key.</returns>
    public static Dictionary<string, string[]> ToJsonKeys(IDictionary<string, string[]> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        var result = new Dictionary<string, string[]>(StringComparer.Ordinal);
        foreach (var (key, messages) in errors)
        {
            var jsonKey = ToJsonKey(key);
            result[jsonKey] = result.TryGetValue(jsonKey, out var existing) ? [.. existing, .. messages] : messages;
        }

        return result;
    }

    private static string ToJsonSegment(string segment)
    {
        var bracket = segment.IndexOf('[', StringComparison.Ordinal);
        var name = bracket < 0 ? segment : segment[..bracket];
        var suffix = bracket < 0 ? string.Empty : segment[bracket..];

        return name.Length == 0 ? segment : JsonNamingPolicy.CamelCase.ConvertName(name) + suffix;
    }
}
