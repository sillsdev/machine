using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SIL.Machine.Morphology.HermitCrab.Conformance;

/// <summary>
/// Deserialized shape of a fixture's manifest.json (see conformance/README.md and
/// docs/conformance-framework-plan.md section 4.2). Field names are camelCase in the JSON file;
/// <see cref="JsonPropertyNameAttribute"/> pins each mapping explicitly rather than relying on a
/// naming-policy convention, so the on-disk contract stays obvious from this class alone.
/// </summary>
public class FixtureManifest
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("category")]
    public string Category { get; set; } = "single-feature";

    [JsonPropertyName("constructs")]
    public List<string> Constructs { get; set; } = new();

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();

    [JsonPropertyName("requires")]
    public List<string> Requires { get; set; } = new();

    // Nullable so Runner can distinguish "field absent" (bool default false would look identical
    // to an explicit "false") from an explicit value to cross-check against Category above.
    [JsonPropertyName("pathological")]
    public bool? Pathological { get; set; }

    [JsonPropertyName("expectCrash")]
    public bool ExpectCrash { get; set; }

    [JsonPropertyName("budget")]
    public FixtureBudget Budget { get; set; }

    [JsonPropertyName("oracle")]
    public JsonElement Oracle { get; set; }

    [JsonPropertyName("provenance")]
    public string Provenance { get; set; } = "";

    private static readonly JsonSerializerOptions Options = new() { ReadCommentHandling = JsonCommentHandling.Skip };

    public static FixtureManifest Load(string path)
    {
        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<FixtureManifest>(json, Options)
            ?? throw new InvalidDataException($"manifest.json at '{path}' deserialized to null");
    }
}

/// <summary>
/// A pathological fixture's complexity budget: a wall-clock bound only (no step-count ceiling
/// exists on this branch -- see conformance framework plan notes on BatchCommand's stripped
/// --rule-stats option). Absent/null for non-pathological fixtures.
/// </summary>
public class FixtureBudget
{
    [JsonPropertyName("wallClockMs")]
    public long WallClockMs { get; set; }
}
