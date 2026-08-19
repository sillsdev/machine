#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using Json.Schema;
using YamlDotNet.RepresentationModel;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

/// <summary>Validates an authored <c>words.yaml</c> against the published words schema.</summary>
/// <remarks>The schema, not the loader, is what a consumer outside this repository can read, so the
/// two must agree on every checked-in fixture. Plain YAML only: aliases, anchors, merge keys and
/// custom tags are refused before conversion rather than silently resolved.</remarks>
public static class WordsSchemaValidation
{
    public const string SchemaRelativePath = "conformance/schema/words.schema.json";

    // A schema registers itself globally under its $id, so it is loaded once per path rather than
    // once per file validated.
    private static readonly Dictionary<string, JsonSchema> LoadedSchemas = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Validation messages for <paramref name="wordsPath"/>; empty when it conforms.</summary>
    public static IReadOnlyList<string> Validate(string wordsPath, string schemaPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(wordsPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaPath);

        JsonNode? node = ReadAsJson(wordsPath);
        if (node is null)
            return new[] { "words.yaml is empty" };

        using var document = System.Text.Json.JsonDocument.Parse(node.ToJsonString());
        EvaluationResults results = Schema(schemaPath)
            .Evaluate(document.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List });
        if (results.IsValid)
            return Array.Empty<string>();

        return (results.Details ?? new List<EvaluationResults>())
            .Where(detail => detail.Errors is { Count: > 0 })
            .SelectMany(detail => detail.Errors!.Select(error => $"{detail.InstanceLocation}: {error.Value}"))
            .DefaultIfEmpty("does not conform to " + SchemaRelativePath)
            .ToArray();
    }

    private static JsonSchema Schema(string schemaPath)
    {
        string key = Path.GetFullPath(schemaPath);
        lock (LoadedSchemas)
        {
            if (!LoadedSchemas.TryGetValue(key, out JsonSchema? schema))
            {
                schema = JsonSchema.FromFile(key);
                LoadedSchemas[key] = schema;
            }

            return schema;
        }
    }

    private static JsonNode? ReadAsJson(string wordsPath)
    {
        var stream = new YamlStream();
        using (var reader = new StreamReader(wordsPath))
            stream.Load(reader);
        if (stream.Documents.Count == 0)
            return null;
        if (stream.Documents.Count != 1)
            throw new InvalidDataException($"{wordsPath}: a words file must contain exactly one YAML document.");

        return Convert(stream.Documents[0].RootNode, wordsPath);
    }

    private static JsonNode? Convert(YamlNode node, string path)
    {
        if (!node.Anchor.IsEmpty)
            throw new InvalidDataException($"{path}: YAML anchors are not admitted.");

        switch (node)
        {
            case YamlMappingNode mapping:
            {
                var result = new JsonObject();
                foreach (KeyValuePair<YamlNode, YamlNode> pair in mapping.Children)
                {
                    if (pair.Key is not YamlScalarNode { Value: { } key })
                        throw new InvalidDataException($"{path}: mapping keys must be scalars.");
                    if (key == "<<")
                        throw new InvalidDataException($"{path}: YAML merge keys are not admitted.");
                    result[key] = Convert(pair.Value, path);
                }

                return result;
            }

            case YamlSequenceNode sequence:
            {
                var result = new JsonArray();
                foreach (YamlNode child in sequence.Children)
                    result.Add(Convert(child, path));
                return result;
            }

            case YamlScalarNode scalar:
                return ConvertScalar(scalar);

            default:
                throw new InvalidDataException($"{path}: unsupported YAML node '{node.GetType().Name}'.");
        }
    }

    /// <summary>Applies YAML 1.1 core scalar resolution for the types the schema distinguishes.</summary>
    private static JsonNode? ConvertScalar(YamlScalarNode scalar)
    {
        string? value = scalar.Value;
        if (value is null)
            return null;
        if (
            scalar.Style
            is YamlDotNet.Core.ScalarStyle.SingleQuoted
                or YamlDotNet.Core.ScalarStyle.DoubleQuoted
                or YamlDotNet.Core.ScalarStyle.Literal
                or YamlDotNet.Core.ScalarStyle.Folded
        )
        {
            return JsonValue.Create(value);
        }

        if (value is "null" or "~" or "")
            return null;
        if (value is "true" or "True" or "TRUE")
            return JsonValue.Create(true);
        if (value is "false" or "False" or "FALSE")
            return JsonValue.Create(false);
        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long integer))
            return JsonValue.Create(integer);

        return JsonValue.Create(value);
    }
}
