using System.Collections.Generic;
using System.IO;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.V2;

/// <summary>
/// One v2 fixture directory: exactly <c>grammar.xml</c> + <c>words.yaml</c>, under
/// <c>conformance/languages/&lt;name&gt;/</c> or <c>conformance/edge-cases/&lt;name&gt;/</c>, per
/// docs/conformance-language-suite-plan.md section 2. Discovery here is deliberately scoped to
/// only those two roots, so it can never collide with a v1 fixture (which lives elsewhere under
/// conformance/ and is keyed off manifest.json, not words.yaml -- see <see cref="Fixture"/>).
/// </summary>
public class FixtureV2(string id, string directory, WordsYaml words)
{
    public string Id { get; } = id;
    public string Directory { get; } = directory;
    public WordsYaml Words { get; } = words;

    public string GrammarPath => Path.Combine(Directory, "grammar.xml");

    public static List<FixtureV2> DiscoverAll(string fixturesRoot)
    {
        var fixtures = new List<FixtureV2>();
        foreach (string category in new[] { "languages", "edge-cases" })
        {
            string categoryRoot = Path.Combine(fixturesRoot, category);
            if (!System.IO.Directory.Exists(categoryRoot))
                continue;

            foreach (string dir in System.IO.Directory.EnumerateDirectories(categoryRoot))
            {
                string grammarPath = Path.Combine(dir, "grammar.xml");
                string wordsYamlPath = Path.Combine(dir, "words.yaml");
                if (!File.Exists(grammarPath) || !File.Exists(wordsYamlPath))
                    continue;

                string name = Path.GetFileName(dir);
                string id = $"{category}/{name}";
                WordsYaml words = WordsYamlLoader.Load(wordsYamlPath);
                fixtures.Add(new FixtureV2(id, dir, words));
            }
        }
        fixtures.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
        return fixtures;
    }
}
