namespace SIL.Machine.AspNetCore.Utils;

internal sealed class DictionaryStringStringConverter : JsonConverter<IReadOnlyDictionary<string, string>>
{
    public override IReadOnlyDictionary<string, string> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException($"JsonTokenType was of type {reader.TokenType}, only objects are supported");
        }

        var dictionary = new Dictionary<string, string>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return dictionary;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("JsonTokenType was not PropertyName");
            }

            var propertyName = reader.GetString();

            if (string.IsNullOrWhiteSpace(propertyName))
            {
                throw new JsonException("Failed to get property name");
            }

            reader.Read();

            dictionary.Add(propertyName!, ExtractValue(ref reader));
        }

        return dictionary;
    }

    public override void Write(
        Utf8JsonWriter writer,
        IReadOnlyDictionary<string, string> value,
        JsonSerializerOptions options
    )
    {
        JsonSerializer.Serialize(writer, value, options);
    }

    private static string ExtractValue(ref Utf8JsonReader reader)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                return reader.GetString() ?? "Error Reading String.";
            case JsonTokenType.False:
                return "false";
            case JsonTokenType.True:
                return "true";
            case JsonTokenType.Null:
                return "null";
            case JsonTokenType.Number:
                if (reader.TryGetDouble(out var result))
                    return result.ToString(CultureInfo.InvariantCulture);
                return "Error Reading Number.";
            default:
                throw new JsonException($"'{reader.TokenType}' is not supported");
        }
    }
}
