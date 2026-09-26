using System.IO;

namespace SIL.Machine.Morphology.HermitCrab.Conformance;

/// <summary>
/// A fixture materialized onto disk in the on-disk adapter shape (grammar.xml + words.txt +
/// expected.tsv + an in-memory <see cref="FixtureManifest"/>), so it can be handed to
/// <see cref="MaterializedRunner"/> and <see cref="IEngine"/> (self-check or an external adapter
/// process). Built by <see cref="FixtureMaterializer"/> from a <see cref="Fixture"/>.
/// </summary>
public class MaterializedFixture(string directory, FixtureManifest manifest)
{
    public string Id { get; } = manifest.Id;
    public string Directory { get; } = directory;
    public FixtureManifest Manifest { get; } = manifest;

    public string GrammarPath => Path.Combine(Directory, "grammar.xml");
    public string WordsPath => Path.Combine(Directory, "words.txt");
    public string ExpectedPath => Path.Combine(Directory, "expected.tsv");
}
