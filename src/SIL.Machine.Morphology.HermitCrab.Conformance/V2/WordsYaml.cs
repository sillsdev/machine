using System.Collections.Generic;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.V2;

/// <summary>
/// In-memory model of one <c>words.yaml</c> file, per
/// docs/conformance-language-suite-plan.md section 2.1. Parsed by <see cref="WordsYamlLoader"/>,
/// which enforces the strict, fixed key vocabulary documented there -- this class only carries
/// already-validated data, it does not itself validate anything.
/// </summary>
public class WordsYaml
{
    public string Language = "";
    public List<string> InspiredBy { get; } = new();
    public List<string> Sources { get; } = new();
    public List<string> Requires { get; } = new();

    /// <summary>Edge-cases only: a wall-clock budget in milliseconds (v1's manifest.json budget.wallClockMs).</summary>
    public long? BudgetMs;

    /// <summary>Edge-cases only: this fixture's ground truth is a crash, not a signature (v1's manifest.json expectCrash).</summary>
    public bool ExpectCrash;

    public List<WordEntry> Words { get; } = new();
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
    /// zero valid analyses) because the two produce different adapter-contract status columns --
    /// "SKIPPED" vs "ok" -- and BatchCommand/expected.tsv distinguish them (v1 row: "N word 0 SKIPPED -").
    /// Both modes verify it: self-check confirms an <see cref="InvalidShapeException"/> was actually
    /// thrown; adapter mode materializes status "SKIPPED". Like expect_fail, a skip word has no parses.
    /// </summary>
    public bool ExpectSkip;

    public List<string> BlockedBy { get; } = new();
    public List<string> Exercises { get; } = new();
    public List<ParseEntry> Parses { get; } = new();
}

public class ParseEntry
{
    public string Signature = "";
    public string Gloss = "";
    public bool Guess;
    public List<string> Rules { get; } = new();
    public List<string> Exercises { get; } = new();
}
