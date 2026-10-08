using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FunBooksAndVideos.Application.Idempotency;

/// <summary>
/// Stable fingerprint of a request payload: the lowercase hex SHA-256 of its canonical JSON form.
/// Two requests with the same fingerprint carry the same payload, so a replay is safe.
/// </summary>
public static class RequestFingerprint
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    /// <summary>Computes the fingerprint. Exclude the idempotency key itself from <paramref name="request"/> before calling.</summary>
    public static string Compute<T>(T request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var json = JsonSerializer.SerializeToUtf8Bytes(request, Options);
        return Convert.ToHexStringLower(SHA256.HashData(json));
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter(), new CanonicalDecimalConverter() },
        };
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }

    /// <summary>
    /// Writes decimals without trailing zeros, so that <c>48.5</c>, <c>48.50</c> and <c>48.500</c> (the same value,
    /// different scales) fingerprint identically and a client that formats a total differently is not rejected.
    /// </summary>
    private sealed class CanonicalDecimalConverter : JsonConverter<decimal>
    {
        // Dividing by 1 with the maximum scale strips trailing zeros from a decimal without changing its value.
        private const decimal ScaleStripper = 1.0000000000000000000000000000m;

        public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.GetDecimal();

        public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
        {
            ArgumentNullException.ThrowIfNull(writer);
            writer.WriteNumberValue(value / ScaleStripper);
        }
    }
}
