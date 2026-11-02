using System.Collections.Generic;

namespace SIL.Machine.Morphology.HermitCrab.Conformance;

/// <summary>
/// In-memory model of one <c>words.yaml</c> file. Parsed by <see cref="WordsYamlLoader"/>, which
/// enforces the strict, fixed key vocabulary -- this class only carries already-validated data, it
/// does not itself validate anything.
/// </summary>
public class WordsYaml
{
    public string Language = "";
    public List<string> InspiredBy { get; } = new();
    public List<string> Sources { get; } = new();
    public List<string> Requires { get; } = new();

    /// <summary>Edge-cases only: a wall-clock budget in milliseconds for the whole fixture.</summary>
    public long? BudgetMs;

    /// <summary>Edge-cases only: this fixture's ground truth is a crash, not a signature.</summary>
    public bool ExpectCrash;

    public bool? FieldworksProducible;

    /// <summary>Required and non-empty when <see cref="FieldworksProducible"/> is false: names the
    /// offending construct(s) and cites the HCLoader evidence grounding the verdict.</summary>
    public string FieldworksProducibleNotes = "";

    /// <summary>The <c>morpher:</c> block; empty when absent, meaning the engine constructor defaults.</summary>
    public MorpherSettings Morpher { get; } = new();

    public List<WordEntry> Words { get; } = new();

    /// <summary>
    /// Named complete runs against the same grammar; coverage remains attached to <see cref="Words"/>.
    /// </summary>
    public List<WordRun> Variants { get; } = new();

    public IEnumerable<WordRun> Runs
    {
        get
        {
            yield return new WordRun("default", Morpher, Words);
            foreach (WordRun variant in Variants)
                yield return variant;
        }
    }
}

public class WordRun(string name, MorpherSettings morpher, List<WordEntry> words)
{
    public string Name { get; } = name;
    public MorpherSettings Morpher { get; } = morpher;
    public List<WordEntry> Words { get; } = words;
}

public class WordEntry
{
    public string Word = "";
    public string Note = "";
    public string Provenance = "";
    public bool ExpectFail;

    /// <summary>
    /// The oracle SKIPS this word rather than returning a genuine zero-parse "ok": it throws
    /// <see cref="InvalidShapeException"/> (e.g. the surface contains a segment undeclared in the
    /// grammar's CharacterDefinitionTable). Distinct from <see cref="ExpectFail"/> (well-formed input,
    /// zero valid analyses) because the two produce different status columns --
    /// "SKIPPED" vs "ok" -- in BatchCommand's output. The runner confirms an <see cref="InvalidShapeException"/>
    /// was actually thrown. Like expect_fail, a skip word has no parses.
    /// </summary>
    public bool ExpectSkip;

    public List<string> BlockedBy { get; } = new();

    /// <summary>Declaration ids (a grammar element's <c>id</c>) whose deactivation this word's failure observes.
    /// The runner does not read it; the loader accepts it because the suite's schema allows it.</summary>
    public List<string> Neutralizes { get; } = new();
    public List<string> Exercises { get; } = new();
    public List<ParseEntry> Parses { get; } = new();

    /// <summary>The <c>cell</c> of each <c>claims</c> entry. The loader checks the entry's shape, since the suite's
    /// schema allows it; the runner does not otherwise read it.</summary>
    public List<string> Claims { get; } = new();
}

public class ParseEntry
{
    public string Signature = "";
    public string Gloss = "";
    public bool Guess;
    public List<string> Rules { get; } = new();
    public List<string> Exercises { get; } = new();
}
