using System.Collections.Generic;
using System.IO;

namespace SIL.Machine.Morphology.HermitCrab.Conformance;

/// <summary>
/// One fixture directory: exactly <c>grammar.xml</c> + <c>words.yaml</c>, under
/// <c>conformance/languages/&lt;name&gt;/</c> or <c>conformance/edge-cases/&lt;name&gt;/</c>.
/// </summary>
public class Fixture(string id, string directory, WordsYaml words)
{
    public string Id { get; } = id;
    public string Directory { get; } = directory;
    public WordsYaml Words { get; } = words;

    public string GrammarPath => Path.Combine(Directory, "grammar.xml");

    /// <summary>The only directories under a fixtures root that hold fixtures.</summary>
    public static readonly string[] Categories = { "languages", "edge-cases" };

    public static List<Fixture> DiscoverAll(string fixturesRoot)
    {
        var fixtures = new List<Fixture>();
        List<StatusException> status = StatusYamlLoader.LoadIfPresent(fixturesRoot);
        foreach (string category in Categories)
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
        AttachStatus(fixtures, status, fixturesRoot);
        return fixtures;
    }

    /// <summary>Hands each <c>status.yaml</c> row to its test case. A whole-case row also becomes an upstream
    /// entry, so the runner's skip rule and the FieldWorks-producible gate read one list. A row naming no test
    /// case, or no word of its test case, is an error: it would otherwise never be seen.</summary>
    private static void AttachStatus(List<Fixture> fixtures, List<StatusException> status, string root)
    {
        foreach (StatusException row in status)
        {
            Fixture target = fixtures.Find(f => f.Id == row.Case);
            if (target == null)
            {
                throw new WordsYamlException(
                    Path.Combine(root, StatusYamlLoader.FileName),
                    $"'{row.Case}' names no test case"
                );
            }
            if (row.Word.Length != 0 && !target.Words.Words.Exists(w => w.Word == row.Word))
            {
                throw new WordsYamlException(
                    Path.Combine(root, StatusYamlLoader.FileName),
                    $"'{row.Case}' has no word '{row.Word}'"
                );
            }
            target.Words.Exceptions.Add(row);
            if (row.Word.Length == 0)
            {
                target.Words.Upstream.Add(row.Upstream);
            }
        }
    }
}
