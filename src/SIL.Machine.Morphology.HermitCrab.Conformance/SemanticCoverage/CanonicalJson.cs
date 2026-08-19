#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

/// <summary>Produces compact UTF-8 JSON with recursively ordinal-sorted object keys.</summary>
internal static class CanonicalJson
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = null,
    };

    internal static byte[] SerializeUtf8<T>(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        using JsonDocument document = JsonDocument.Parse(JsonSerializer.SerializeToUtf8Bytes(value, SerializerOptions));
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false }))
            WriteElement(document.RootElement, writer);
        return stream.ToArray();
    }

    internal static string Serialize<T>(T value) => Encoding.UTF8.GetString(SerializeUtf8(value));

    internal static byte[] NormalizeJson(ReadOnlySpan<byte> utf8)
    {
        using JsonDocument document = JsonDocument.Parse(utf8.ToArray());
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false }))
            WriteElement(document.RootElement, writer);
        return stream.ToArray();
    }

    private static void WriteElement(JsonElement element, Utf8JsonWriter writer)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                var properties = new List<JsonProperty>();
                var propertyNames = new HashSet<string>(StringComparer.Ordinal);
                foreach (JsonProperty property in element.EnumerateObject())
                {
                    if (!propertyNames.Add(property.Name))
                        throw new InvalidDataException($"Duplicate JSON property '{property.Name}'.");
                    properties.Add(property);
                }
                properties.Sort((left, right) => StringComparer.Ordinal.Compare(left.Name, right.Name));
                foreach (JsonProperty property in properties)
                {
                    writer.WritePropertyName(property.Name);
                    WriteElement(property.Value, writer);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (JsonElement item in element.EnumerateArray())
                    WriteElement(item, writer);
                writer.WriteEndArray();
                break;
            case JsonValueKind.String:
                writer.WriteStringValue(NormalizeString(element.GetString()!));
                break;
            case JsonValueKind.Number:
                if (!decimal.TryParse(element.GetRawText(), NumberStyles.Float, CultureInfo.InvariantCulture, out decimal decimalValue))
                    throw new InvalidDataException($"JSON number '{element.GetRawText()}' is outside the supported canonical range.");
                writer.WriteRawValue(decimalValue.ToString("G29", CultureInfo.InvariantCulture), skipInputValidation: true);
                break;
            case JsonValueKind.True:
                writer.WriteBooleanValue(true);
                break;
            case JsonValueKind.False:
                writer.WriteBooleanValue(false);
                break;
            case JsonValueKind.Null:
                writer.WriteNullValue();
                break;
            default:
                throw new InvalidDataException($"Unsupported JSON value kind '{element.ValueKind}'.");
        }
    }

    private static string NormalizeString(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
}
