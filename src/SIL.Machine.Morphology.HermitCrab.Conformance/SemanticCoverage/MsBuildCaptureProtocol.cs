using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

internal sealed record CapturedCompilerItem(
    string Identity,
    IReadOnlyDictionary<string, string> Metadata);

internal sealed record CapturedCompilerInputs(
    IReadOnlyDictionary<string, string> Properties,
    IReadOnlyDictionary<string, IReadOnlyList<CapturedCompilerItem>> Items);

internal static class MsBuildCaptureProtocol
{
    internal const string Version = "hc-semantic-msbuild/v1";

    private static readonly string[] RequiredProperties =
    {
        "PanGlossCompilerInputProtocol", "MSBuildAllProjects", "AssemblyName", "TargetFramework",
        "LangVersion", "Nullable", "DefineConstants", "AllowUnsafeBlocks", "CheckForOverflowUnderflow",
        "OutputType", "NETCoreSdkVersion", "MSBuildVersion", "CscToolPath", "RoslynAssembliesPath",
        "GeneratedAssemblyInfoFile", "TargetFrameworkMonikerAssemblyAttributesPath",
    };

    private static readonly string[] RequiredItemFamilies =
    {
        "CscCommandLineArgs", "Compile", "ProjectReference", "ReferencePathWithRefAssemblies",
        "Analyzer", "AdditionalFiles", "EditorConfigFiles", "Using",
    };

    internal static CapturedCompilerInputs Parse(ReadOnlySpan<byte> utf8Json)
    {
        if (utf8Json.IsEmpty)
            throw new InvalidDataException("MSBuild capture output is empty.");

        using JsonDocument document = ParseDocument(utf8Json);
        JsonElement root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("MSBuild capture output must be a JSON object.");

        JsonElement properties = RequiredObject(root, "Properties");
        JsonElement items = RequiredObject(root, "Items");
        var propertyValues = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string name in RequiredProperties)
        {
            JsonElement value = RequiredProperty(properties, name);
            if (value.ValueKind != JsonValueKind.String)
                throw new InvalidDataException($"MSBuild property '{name}' must be a string.");
            propertyValues.Add(name, value.GetString() ?? throw new InvalidDataException($"MSBuild property '{name}' is null."));
        }

        if (!string.Equals(propertyValues["PanGlossCompilerInputProtocol"], Version, StringComparison.Ordinal))
            throw new InvalidDataException($"Unexpected MSBuild capture protocol '{propertyValues["PanGlossCompilerInputProtocol"]}'.");

        foreach (JsonProperty property in items.EnumerateObject())
        {
            if (!Array.Exists(RequiredItemFamilies, family => string.Equals(family, property.Name, StringComparison.Ordinal)))
                throw new InvalidDataException($"Unknown MSBuild item family '{property.Name}'.");
        }

        var itemValues = new Dictionary<string, IReadOnlyList<CapturedCompilerItem>>(StringComparer.Ordinal);
        foreach (string family in RequiredItemFamilies)
        {
            JsonElement familyValue = RequiredProperty(items, family);
            if (familyValue.ValueKind != JsonValueKind.Array)
                throw new InvalidDataException($"MSBuild item family '{family}' must be an array.");

            var parsed = new List<CapturedCompilerItem>();
            foreach (JsonElement item in familyValue.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                    throw new InvalidDataException($"MSBuild item family '{family}' contains a non-object item.");
                JsonElement identity = RequiredProperty(item, "Identity");
                if (identity.ValueKind != JsonValueKind.String || string.IsNullOrEmpty(identity.GetString()))
                    throw new InvalidDataException($"MSBuild item family '{family}' contains a null or empty Identity.");

                var metadata = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (JsonProperty property in item.EnumerateObject())
                {
                    if (property.NameEquals("Identity"))
                        continue;
                    if (property.Value.ValueKind != JsonValueKind.String)
                        throw new InvalidDataException($"Metadata '{property.Name}' on item family '{family}' must be a string.");
                    metadata.Add(property.Name, property.Value.GetString() ?? throw new InvalidDataException($"Metadata '{property.Name}' is null."));
                }

                parsed.Add(new CapturedCompilerItem(identity.GetString()!, metadata));
            }

            itemValues.Add(family, parsed);
        }

        return new CapturedCompilerInputs(propertyValues, itemValues);
    }

    private static JsonDocument ParseDocument(ReadOnlySpan<byte> utf8Json)
    {
        try
        {
            var reader = new Utf8JsonReader(utf8Json, isFinalBlock: true, state: default);
            EnsureNoDuplicateProperties(ref reader);
            return JsonDocument.Parse(utf8Json.ToArray());
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("MSBuild capture output is not valid UTF-8 JSON.", exception);
        }
    }

    private static void EnsureNoDuplicateProperties(ref Utf8JsonReader reader)
    {
        var objectPropertyNames = new Stack<HashSet<string>>();
        int rootValues = 0;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                if (objectPropertyNames.Count == 0)
                {
                    rootValues++;
                }
                objectPropertyNames.Push(new HashSet<string>(StringComparer.Ordinal));
            }
            else if (reader.TokenType == JsonTokenType.StartArray)
            {
                if (objectPropertyNames.Count == 0)
                {
                    rootValues++;
                }
            }
            else if (reader.TokenType == JsonTokenType.PropertyName)
            {
                if (objectPropertyNames.Count == 0 || !objectPropertyNames.Peek().Add(reader.GetString()!))
                {
                    throw new InvalidDataException($"Duplicate JSON property '{reader.GetString()}'.");
                }
            }
            else if (reader.TokenType == JsonTokenType.EndObject)
            {
                objectPropertyNames.Pop();
            }
        }

        if (rootValues != 1 || objectPropertyNames.Count != 0)
            throw new InvalidDataException("MSBuild capture output must contain exactly one complete JSON value.");
    }

    private static JsonElement RequiredObject(JsonElement parent, string name)
    {
        JsonElement value = RequiredProperty(parent, name);
        if (value.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException($"MSBuild capture member '{name}' must be an object.");
        return value;
    }

    private static JsonElement RequiredProperty(JsonElement parent, string name)
    {
        if (!parent.TryGetProperty(name, out JsonElement value))
            throw new InvalidDataException($"MSBuild capture member '{name}' is missing.");
        return value;
    }
}
