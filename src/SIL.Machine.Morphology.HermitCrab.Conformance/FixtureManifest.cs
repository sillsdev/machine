using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SIL.Machine.Morphology.HermitCrab.Conformance;

/// <summary>
/// In-memory adapter-mode fixture descriptor, built by <see cref="FixtureMaterializer"/> from a
/// fixture's grammar.xml + words.yaml. <see cref="JsonPropertyNameAttribute"/> is retained on each
/// field only because <see cref="Oracle"/> is a raw <see cref="JsonElement"/>; nothing in this
/// codebase deserializes a <c>FixtureManifest</c> from JSON on disk.
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
