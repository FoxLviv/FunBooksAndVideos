using System.Collections.Frozen;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FunBooksAndVideos.Api.Json;

/// <summary>
/// Serialises enums that are not <see cref="FlagsAttribute"/> enums as exactly one declared member name.
/// Unlike <see cref="JsonStringEnumConverter"/>, it rejects numbers, numeric strings (<c>"3"</c>) and
/// comma-separated names (<c>"BookClub, VideoClub"</c>), which the built-in converter would otherwise
/// combine into a different, possibly valid, member. Flags enums are left to the built-in converter.
/// </summary>
internal sealed class StrictStringEnumConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        ArgumentNullException.ThrowIfNull(typeToConvert);

        return typeToConvert.IsEnum && !typeToConvert.IsDefined(typeof(FlagsAttribute), inherit: false);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(typeToConvert);

        var converterType = typeof(StrictStringEnumConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }

    /// <summary>Reads and writes a single, declared member name of <typeparamref name="TEnum"/>.</summary>
    private sealed class StrictStringEnumConverter<TEnum> : JsonConverter<TEnum>
        where TEnum : struct, Enum
    {
        // Exact member names only (case-insensitive): numeric strings and comma-separated combinations,
        // which Enum.Parse would accept and turn into some other member, are never found here.
        private static readonly FrozenDictionary<string, TEnum> ByName =
            Enum.GetValues<TEnum>().ToFrozenDictionary(value => value.ToString(), value => value, StringComparer.OrdinalIgnoreCase);

        private static readonly string AllowedNames = string.Join(", ", ByName.Keys);

        public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException($"A {typeof(TEnum).Name} must be given as one of its names: {AllowedNames}.");
            }

            var value = reader.GetString();
            if (value is not null && ByName.TryGetValue(value, out var parsed))
            {
                return parsed;
            }

            throw new JsonException($"'{value}' is not a valid {typeof(TEnum).Name}.");
        }

        public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
        {
            ArgumentNullException.ThrowIfNull(writer);

            var name = Enum.GetName(value)
                ?? throw new JsonException($"'{value}' is not a declared {typeof(TEnum).Name} member.");
            writer.WriteStringValue(name);
        }
    }
}
