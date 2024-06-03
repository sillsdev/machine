namespace SIL.Machine.AspNetCore.Models;

public enum ClearMLTaskStatus
{
    Created,
    Queued,
    InProgress,
    Stopped,
    Published,
    Publishing,
    Closed,
    Failed,
    Completed,
    Unknown
}

public record ClearMLTask
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required ClearMLProject Project { get; init; }
    public required ClearMLTaskStatus Status { get; init; }
    public string? StatusReason { get; init; }
    public string? StatusMessage { get; init; }
    public required DateTime Created { get; init; }
    public int? LastIteration { get; init; }
    public int ActiveDuration { get; init; }
    public required IReadOnlyDictionary<
        string,
        IReadOnlyDictionary<string, ClearMLMetricsEvent>
    > LastMetrics { get; init; }

    [JsonConverter(typeof(DictionaryStringStringConverter))]
    public required IReadOnlyDictionary<string, string> Runtime { get; init; }
}

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
