using System.IO;

namespace SIL.Machine.Morphology.HermitCrab.Conformance;

/// <summary>
/// A fixture materialized onto disk in the on-disk adapter shape (grammar.xml + words.txt +
/// expected.tsv + an in-memory <see cref="FixtureManifest"/>), so it can be handed to
/// <see cref="MaterializedRunner"/> and <see cref="IEngine"/> (self-check or an external adapter
/// process). Built by <see cref="FixtureMaterializer"/> from a <see cref="Fixture"/>.
/// </summary>
public class MaterializedFixture(
    string directory,
    FixtureManifest manifest,
    string variantName = null,
    bool? requiresMorpherSettings = null
)
{
    public string Id { get; } = variantName == null ? manifest.Id : $"{manifest.Id} [variant {variantName}]";
    public string Directory { get; } = directory;
    public FixtureManifest Manifest { get; } = manifest;

    /// <summary>
    /// Fixture-level capability requirement. This can be true for the default run even when its own
    /// settings are empty, because one of the fixture's named variants declares settings.
    /// </summary>
    public bool RequiresMorpherSettings { get; } = requiresMorpherSettings ?? !manifest.Morpher.IsEmpty;

    public string GrammarPath => Path.Combine(Directory, "grammar.xml");
    public string WordsPath => Path.Combine(Directory, "words.txt");
    public string ExpectedPath => Path.Combine(Directory, "expected.tsv");

    /// <summary>
    /// The JSON object of this run's <c>morpher:</c> block, keyed by FieldWorks element name; <c>{}</c> when absent.
    /// </summary>
    public const string MorpherFileName = "morpher.json";

    public string MorpherPath => Path.Combine(Directory, MorpherFileName);
}
