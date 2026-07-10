using System.Collections.Generic;
using System.IO;

namespace SIL.Machine.Morphology.HermitCrab.Conformance;

/// <summary>One fixture directory under conformance/: its id, path, and parsed manifest.</summary>
public class Fixture(string directory, FixtureManifest manifest)
{
    public string Id { get; } = manifest.Id;
    public string Directory { get; } = directory;
    public FixtureManifest Manifest { get; } = manifest;

    public string GrammarPath => Path.Combine(Directory, "grammar.xml");
    public string WordsPath => Path.Combine(Directory, "words.txt");
    public string ExpectedPath => Path.Combine(Directory, "expected.tsv");

    /// <summary>
    /// Discovers every fixture under <paramref name="fixturesRoot"/>: any directory (at any depth)
    /// containing a manifest.json is treated as one fixture.
    /// </summary>
    public static List<Fixture> DiscoverAll(string fixturesRoot)
    {
        var fixtures = new List<Fixture>();
        foreach (
            string manifestPath in System.IO.Directory.EnumerateFiles(
                fixturesRoot,
                "manifest.json",
                SearchOption.AllDirectories
            )
        )
        {
            string dir = Path.GetDirectoryName(manifestPath)!;
            FixtureManifest manifest = FixtureManifest.Load(manifestPath);
            fixtures.Add(new Fixture(dir, manifest));
        }
        fixtures.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
        return fixtures;
    }
}
