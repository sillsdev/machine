#nullable enable
using System.Collections.Generic;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

/// <summary>Provenance and integrity for the conformance fixtures PanGloss imports.</summary>
/// <remarks>Deliberately not a second copy of the ground truth. <c>words.yaml</c> is the single
/// canonical authored format — it carries the reasoning for each fixture in comments, which no
/// generated JSON can hold — and <c>grammar.xml</c> is the authoritative grammar. This manifest
/// records what those files are, which format each conforms to, and the hash a consumer verifies
/// them against.</remarks>
public sealed record ConformanceManifest(
    string FormatVersion,
    string WordsAuthoringFormatVersion,
    string GrammarFormatVersion,
    string DtdPath,
    string DtdSha256,
    string SourceHash,
    IReadOnlyList<ManifestFixture> Fixtures)
{
    public const string ManifestFormat = "hc-conformance-manifest/v1";
    public const string WordsFormat = "hc-conformance-words/v1";
    public const string GrammarFormat = "sil-machine-hermit-crab-input-xml/v1";
}

/// <param name="CaseCount">Cases authored in <c>words.yaml</c>, so a consumer can detect a partial
/// read without parsing the file.</param>
public sealed record ManifestFixture(
    string FixtureId,
    string Category,
    string DisplayLanguage,
    string GrammarPath,
    string GrammarSha256,
    string WordsPath,
    string WordsSha256,
    int CaseCount,
    bool ExpectedCrash);

/// <summary>Canonical serialization of the manifest.</summary>
public static class ManifestJson
{
    public static string Serialize(ConformanceManifest manifest) => CanonicalJson.Serialize(manifest);

    public static byte[] SerializeUtf8(ConformanceManifest manifest) => CanonicalJson.SerializeUtf8(manifest);
}
