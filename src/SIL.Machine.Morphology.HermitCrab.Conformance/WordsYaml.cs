using System.Collections.Generic;

namespace SIL.Machine.Morphology.HermitCrab.Conformance;

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

    /// <summary>Edge-cases only: a wall-clock budget in milliseconds (materialized as manifest.json's budget.wallClockMs).</summary>
    public long? BudgetMs;

    /// <summary>Edge-cases only: this fixture's ground truth is a crash, not a signature (materialized as manifest.json's expectCrash).</summary>
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
    /// "SKIPPED" vs "ok" -- and BatchCommand/expected.tsv distinguish them (materialized row: "N word 0 SKIPPED -").
    /// Both modes verify it: self-check confirms an <see cref="InvalidShapeException"/> was actually
    /// thrown; adapter mode materializes status "SKIPPED". Like expect_fail, a skip word has no parses.
    /// </summary>
    public bool ExpectSkip;

    public List<string> BlockedBy { get; } = new();

    /// <summary>
    /// Declaration ids (a grammar element's <c>id</c>) whose deactivation this word's failure
    /// observes. Without it, a deactivated declaration would inherit credit from any failing word in
    /// the same fixture, which is how a decoy nothing targets came to be graded as a control.
    /// </summary>
    public List<string> Neutralizes { get; } = new();
    public List<string> Exercises { get; } = new();
    public List<ParseEntry> Parses { get; } = new();

    /// <summary>
    /// Cell ids from conformance/dataflow-obligations.tsv this word is meant to witness -- checked by
    /// DataflowClaimGate, never trusted by it. A claim can carry the literal machine evidence
    /// (<see cref="ClaimedCellEntry.Before"/>/<see cref="ClaimedCellEntry.After"/>) it was reviewed
    /// against, inline and human-readable, so DataflowClaimGate can recompute the severance and a
    /// reviewer can read the same values without joining four files by hand.
    /// </summary>
    public List<ClaimedCellEntry> ClaimedCells { get; } = new();
}

public class ClaimedCellEntry
{
    public string Cell = "";

    /// <summary>Plain-English description of what is removed (e.g. "ruleFeatures on LexicalEntry
    /// VOKAD"). Read by a reviewer; not itself re-checked -- DataflowClaimGate derives which
    /// element/attribute to sever from the ledger row, not from this text.</summary>
    public string Severing = "";

    /// <summary>The word's parse outcome (SignatureFormat's "status::signature" form) BEFORE either the
    /// writer or the reader is severed. Empty means no inline evidence has been recorded for this claim
    /// yet -- not a defect, just unreviewed.</summary>
    public string Before = "";

    /// <summary>The word's parse outcome AFTER severing either the writer or the reader (a genuine
    /// pair witness means both severances produce the same After). DataflowClaimGate recomputes both
    /// severances against the CURRENT grammar and treats any mismatch -- of Before or of either After --
    /// as a stale claim: reviewed once, against evidence that has since moved.</summary>
    public string After = "";

    /// <summary>
    /// The plain control word (docs/coverage-strategy.md's "every claim needs a control") that
    /// demonstrates this cell's condition is not vacuous -- MC/DC independence needs a second case
    /// differing in exactly this one condition. DataflowClaimGate checks the named word exists in the
    /// same fixture and that its outcome differs from this word's; it does not (yet) confirm the
    /// grammar difference between them is the single named condition.
    /// </summary>
    public string DistinctFrom = "";

    /// <summary>Prose: why this word demonstrates this cell. Read by a reviewer; not mechanically
    /// checked.</summary>
    public string Proof = "";

    /// <summary>Whether the four review fields (Severing/Before/After/Proof) are present as a bundle --
    /// the loader enforces all-or-nothing, so checking any one of them is equivalent to checking all.</summary>
    public bool HasReviewBundle => Before.Length != 0;
}

public class ParseEntry
{
    public string Signature = "";
    public string Gloss = "";
    public bool Guess;
    public List<string> Rules { get; } = new();
    public List<string> Exercises { get; } = new();
}
