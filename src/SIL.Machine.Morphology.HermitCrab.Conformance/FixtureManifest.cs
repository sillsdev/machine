using System.Collections.Generic;

namespace SIL.Machine.Morphology.HermitCrab.Conformance;

/// <summary>
/// In-memory adapter-mode fixture descriptor, built by <see cref="FixtureMaterializer"/> from a
/// fixture's grammar.xml + words.yaml. It is never read from or written to disk.
/// </summary>
public class FixtureManifest
{
    public string Id { get; set; } = "";

    public string Category { get; set; } = "single-feature";

    public List<string> Requires { get; set; } = new();

    /// <summary>The fixture's <c>morpher:</c> block; an engine runs this fixture only if it declares
    /// <see cref="MorpherSettings.Capability"/>, and must apply every value.</summary>
    public MorpherSettings Morpher { get; set; } = new();

    public bool ExpectCrash { get; set; }

    public FixtureBudget Budget { get; set; }

    public string Provenance { get; set; } = "";
}

/// <summary>
/// A pathological fixture's complexity budget: a wall-clock bound only, with no step-count ceiling.
/// Absent/null for non-pathological fixtures.
/// </summary>
public class FixtureBudget
{
    public long WallClockMs { get; set; }
}
