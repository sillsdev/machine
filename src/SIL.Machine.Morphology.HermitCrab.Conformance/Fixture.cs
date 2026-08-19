using System.Collections.Generic;
using System.IO;

namespace SIL.Machine.Morphology.HermitCrab.Conformance;

/// <summary>
/// One fixture directory: exactly <c>grammar.xml</c> + <c>words.yaml</c>, under
/// <c>conformance/languages/&lt;name&gt;/</c> or <c>conformance/edge-cases/&lt;name&gt;/</c>, per
/// docs/conformance-language-suite-plan.md section 2. Discovery is scoped to only those two roots.
/// </summary>
public class Fixture(string id, string directory, WordsYaml words)
{
    public string Id { get; } = id;
    public string Directory { get; } = directory;
    public WordsYaml Words { get; } = words;

    public string GrammarPath => Path.Combine(Directory, "grammar.xml");

    public static List<Fixture> DiscoverAll(string fixturesRoot)
    {
        var fixtures = new List<Fixture>();
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
                fixtures.Add(new Fixture(id, dir, words));
            }
        }
        fixtures.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
        return fixtures;
    }
}
