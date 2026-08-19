#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

/// <summary>
/// Whether a claimed cell id checks out against the checked-in ledger. This is the ONLY axis that fails
/// a build -- see <see cref="DataflowClaimReport.AllClaimsValid"/>.
/// </summary>
public enum DataflowClaimValidity
{
    /// <summary>The cell id exists in conformance/dataflow-obligations.tsv and its status is Satisfied.</summary>
    Valid,

    /// <summary>No row in the ledger has this cell id -- a typo, or an id orphaned when a chain's schema
    /// shape changed.</summary>
    UnknownCellId,

    /// <summary>The cell id exists but the ledger's status for it is not Satisfied -- the author
    /// intended a witness and did not get one.</summary>
    NotSatisfied,
}

/// <summary>
/// Whether a claim's optional inline review bundle (<see cref="ClaimedCellEntry.Before"/>/
/// <see cref="ClaimedCellEntry.After"/>) still matches a fresh recomputation of the severance it
/// describes. A SEPARATE axis from <see cref="DataflowClaimValidity"/>: witnessed (the ledger's
/// pair-witness join says the cell is Satisfied) and reviewed (a human recorded the specific ROLE
/// attribution -- that this word really demonstrates e.g. PresentGatedForm, with the exact before/after
/// parse to show it) are different facts. Collapsing them would let a stale review read as an active
/// one, the same presence-vs-witness confusion a severance sweep elsewhere in this suite already
/// guards against -- this is that rule one level up, for witnessed-vs-reviewed.
/// </summary>
public enum DataflowClaimReviewStatus
{
    /// <summary>No severing/before/after/proof recorded on this claim yet. Not a defect -- most claims
    /// start here.</summary>
    Unreviewed,

    /// <summary>Recomputing BOTH the writer's and the reader's severance against the CURRENT grammar,
    /// for THIS word, reproduces the claimed before/after exactly.</summary>
    Reviewed,

    /// <summary>
    /// A review bundle was recorded but recomputation no longer agrees with it -- the grammar moved,
    /// the engine changed, or the named attribute no longer occurs in this fixture at all. Reported
    /// loudly, never a build failure by itself: a stale review means the review needs to happen again,
    /// not that the cell stopped being covered.
    /// </summary>
    Stale,
}

/// <summary>One severance recomputation for one word: the writer or reader attribute named by the
/// caller's <see cref="RecomputeSeverance"/> arguments is stripped from a copy of the fixture's grammar
/// (a null element/attribute means "recompute the unsevered baseline instead") and the word is
/// reparsed. <paramref name="Outcome"/> is null exactly when recomputation could not be attempted or
/// did not finish -- <paramref name="Error"/> then explains why, and DataflowClaimGate treats that as
/// Stale, never as a silent pass.</summary>
public sealed record SeveranceRecomputation(string? Outcome, string? Error);

public delegate SeveranceRecomputation RecomputeSeverance(
    Fixture fixture,
    string? severedElement,
    string? severedAttribute,
    string word
);

public sealed record DataflowClaimResult(
    string FixtureId,
    string Word,
    string CellId,
    DataflowClaimValidity Validity,
    string Detail,
    DataflowClaimReviewStatus Review,
    string ReviewDetail,
    bool? DistinctFromVerified,
    string DistinctFromDetail
);

/// <summary>A Satisfied ledger cell no claimed_cells: entry names, with the witnessing (fixture, word)
/// the ledger's own evidence text names -- read back for a human to act on, never used to fail anything
/// (see <see cref="DataflowClaimReport.AllClaimsValid"/>'s doc comment).</summary>
public sealed record UnclaimedSatisfiedCell(string CellId, string FixtureId, string WitnessWord);

public sealed record DataflowClaimReport(
    IReadOnlyList<DataflowClaimResult> Claims,
    IReadOnlyList<UnclaimedSatisfiedCell> UnclaimedSatisfiedCells
)
{
    /// <summary>
    /// The one thing that fails a build: every claimed cell id exists in the ledger and its status is
    /// Satisfied. Review staleness deliberately never appears here, and neither does
    /// <see cref="UnclaimedSatisfiedCells"/> -- both are reports, not gates, per this class's own doc
    /// comment.
    /// </summary>
    public bool AllClaimsValid => Claims.All(c => c.Validity == DataflowClaimValidity.Valid);
}

/// <summary>
/// Checks every word's <c>claimed_cells:</c> entries (<see cref="WordEntry.ClaimedCells"/>) against the
/// checked-in <see cref="DataflowObligationLedger"/>. The whole point of the field is that a claim is
/// authored INTENT and intent is only worth anything checked against outcome -- so this gate never
/// marks a cell covered because a word claims it.
/// <see cref="DataflowObligationLedger"/>'s own pair-witness join is the sole authority on
/// <see cref="ObligationStatus.Satisfied"/>; this class only cross-checks a claim against that
/// already-computed fact, in the direction the field exists to catch:
/// <list type="bullet">
/// <item>a claimed cell id absent from the ledger fails (<see cref="DataflowClaimValidity.UnknownCellId"/>);</item>
/// <item>a claimed cell whose ledger status is not Satisfied fails
/// (<see cref="DataflowClaimValidity.NotSatisfied"/>) -- the field's entire reason to exist;</item>
/// <item>a Satisfied cell no word claims is reported via
/// <see cref="DataflowClaimReport.UnclaimedSatisfiedCells"/>, never failed -- a documentation gap, not a
/// correctness one, and failing it now would force mass authoring before anything can go green.</item>
/// </list>
///
/// <para>
/// A claim's optional inline review bundle (<see cref="ClaimedCellEntry.Severing"/>/
/// <see cref="ClaimedCellEntry.Before"/>/<see cref="ClaimedCellEntry.After"/>/
/// <see cref="ClaimedCellEntry.Proof"/>) is checked as a third, independent axis
/// (<see cref="DataflowClaimReviewStatus"/>): the literal before/after parse outcomes are RECOMPUTED --
/// severing the writer and, separately, the reader (the element/attribute pair comes from the ledger
/// row, never from the claim's own text) and reparsing this one word -- and compared against what the
/// claim recorded. A hash would only detect that the evidence moved; the literal values do the same
/// staleness detection AND let a reviewer read them directly, which is the entire reason this is inline
/// text rather than a hash. A mismatch (or an attribute that no longer occurs in this fixture to sever)
/// is <see cref="DataflowClaimReviewStatus.Stale"/>, reported loudly, and NEVER silently treated as
/// reviewed. Recomputation is never used to grant <see cref="ObligationStatus.Satisfied"/> -- only the
/// ledger's own pair-witness join does that.
/// </para>
///
/// <para>
/// <c>distinct_from</c> (MC/DC independence -- every claim needs a control) is checked structurally,
/// independent of the review bundle: the named word must exist in the same
/// fixture and its outcome (currently: <see cref="WordEntry.ExpectFail"/>) must differ from this word's.
/// This is deliberately the existence-plus-outcome-difference floor, not the fuller check of whether the
/// grammar difference between the two words is exactly the one named condition -- see this method's own
/// remarks in the class that calls it for what is left undone.
/// </para>
/// </summary>
public static class DataflowClaimGate
{
    private static readonly TimeSpan RecomputeTimeout = TimeSpan.FromSeconds(20);

    /// <summary>
    /// The real severance recomputation: strips <paramref name="severedElement"/>.<paramref
    /// name="severedAttribute"/> from a copy of the fixture's grammar (or, when either is null, leaves
    /// the grammar unsevered -- the baseline run) and reparses exactly <paramref name="word"/>, in the
    /// same killable child process every other counterfactual run in this directory uses.
    /// </summary>
    public static SeveranceRecomputation DefaultRecompute(
        Fixture fixture,
        string? severedElement,
        string? severedAttribute,
        string word
    )
    {
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentNullException.ThrowIfNull(word);

        string grammarPath = fixture.GrammarPath;
        string? scratchPath = null;
        try
        {
            if (severedElement is not null && severedAttribute is not null)
            {
                XDocument grammar = XDocument.Load(fixture.GrammarPath);
                XDocument? severed = InterfaceWitnessGate.Sever(grammar, severedElement, severedAttribute, out int removedCount);
                if (severed is null)
                {
                    return new SeveranceRecomputation(
                        null,
                        $"'{severedElement}.{severedAttribute}' does not occur in this fixture's grammar to sever"
                    );
                }

                string scratchDirectory = Path.Combine(Path.GetTempPath(), "hc-dataflow-claim-gate");
                Directory.CreateDirectory(scratchDirectory);
                scratchPath = Path.Combine(scratchDirectory, $"claim-{Guid.NewGuid():N}.xml");
                string dtdSource = Path.Combine(Path.GetDirectoryName(fixture.GrammarPath)!, "HermitCrabInput.dtd");
                if (File.Exists(dtdSource))
                    File.Copy(dtdSource, Path.Combine(scratchDirectory, "HermitCrabInput.dtd"), overwrite: true);
                severed.Save(scratchPath);
                grammarPath = scratchPath;
            }

            IReadOnlyList<string> outcomes = CounterfactualGate.EvaluateWithTimeout(
                grammarPath,
                new[] { word },
                RecomputeTimeout
            );
            return new SeveranceRecomputation(outcomes[0], null);
        }
        catch (TimeoutException ex)
        {
            return new SeveranceRecomputation(null, ex.Message);
        }
        catch (Exception ex)
        {
            return new SeveranceRecomputation(null, $"{ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            if (scratchPath is not null && File.Exists(scratchPath))
            {
                try
                {
                    File.Delete(scratchPath);
                }
                catch (IOException)
                {
                    // Mirrors InterfaceWitnessGate.Evaluate: a killed timed-out worker may still hold
                    // the file open, and blocking on that thread is worse than leaving one scratch file.
                }
            }
        }
    }

    public static DataflowClaimReport Evaluate(
        IReadOnlyList<Fixture> fixtures,
        IReadOnlyList<DataflowObligationLedger.Row> ledger,
        RecomputeSeverance? recompute = null
    )
    {
        ArgumentNullException.ThrowIfNull(fixtures);
        ArgumentNullException.ThrowIfNull(ledger);
        RecomputeSeverance evaluate = recompute ?? DefaultRecompute;

        Dictionary<string, DataflowObligationLedger.Row> byCellId = ledger.ToDictionary(
            r => r.CellId,
            StringComparer.Ordinal
        );

        var claims = new List<DataflowClaimResult>();
        var claimedCellIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (Fixture fixture in fixtures)
        {
            Dictionary<string, WordEntry> wordsByName = fixture.Words.Words.ToDictionary(w => w.Word, StringComparer.Ordinal);

            foreach (WordEntry word in fixture.Words.Words)
            {
                foreach (ClaimedCellEntry claim in word.ClaimedCells)
                {
                    claimedCellIds.Add(claim.Cell);

                    if (!byCellId.TryGetValue(claim.Cell, out DataflowObligationLedger.Row? row))
                    {
                        claims.Add(
                            new DataflowClaimResult(
                                fixture.Id,
                                word.Word,
                                claim.Cell,
                                DataflowClaimValidity.UnknownCellId,
                                $"'{claim.Cell}' does not exist in {DataflowObligationLedger.RelativePath}",
                                DataflowClaimReviewStatus.Unreviewed,
                                "cell id is unknown; review cannot be evaluated",
                                null,
                                ""
                            )
                        );
                        continue;
                    }

                    if (row.Status != ObligationStatus.Satisfied)
                    {
                        claims.Add(
                            new DataflowClaimResult(
                                fixture.Id,
                                word.Word,
                                claim.Cell,
                                DataflowClaimValidity.NotSatisfied,
                                $"ledger status is {row.Status}, not Satisfied: {row.Evidence}",
                                DataflowClaimReviewStatus.Unreviewed,
                                "cell is not Satisfied; review cannot be evaluated",
                                null,
                                ""
                            )
                        );
                        continue;
                    }

                    (DataflowClaimReviewStatus review, string reviewDetail) = EvaluateReview(
                        claim,
                        row,
                        fixture,
                        word.Word,
                        evaluate
                    );
                    (bool? distinctFromVerified, string distinctFromDetail) = EvaluateDistinctFrom(claim, word, wordsByName);

                    claims.Add(
                        new DataflowClaimResult(
                            fixture.Id,
                            word.Word,
                            claim.Cell,
                            DataflowClaimValidity.Valid,
                            "matches a Satisfied cell in the ledger",
                            review,
                            reviewDetail,
                            distinctFromVerified,
                            distinctFromDetail
                        )
                    );
                }
            }
        }

        var unclaimed = new List<UnclaimedSatisfiedCell>();
        foreach (DataflowObligationLedger.Row row in ledger.Where(r => r.Status == ObligationStatus.Satisfied))
        {
            if (claimedCellIds.Contains(row.CellId))
                continue;
            unclaimed.Add(new UnclaimedSatisfiedCell(row.CellId, ExtractWitnessFixture(row.Evidence), ExtractWitnessWord(row.Evidence)));
        }

        return new DataflowClaimReport(claims, unclaimed);
    }

    private static (DataflowClaimReviewStatus, string) EvaluateReview(
        ClaimedCellEntry claim,
        DataflowObligationLedger.Row row,
        Fixture fixture,
        string word,
        RecomputeSeverance evaluate
    )
    {
        if (!claim.HasReviewBundle)
            return (DataflowClaimReviewStatus.Unreviewed, "no severing/before/after/proof recorded on this claim yet");

        SeveranceRecomputation baseline = evaluate(fixture, null, null, word);
        if (baseline.Outcome is null)
            return (DataflowClaimReviewStatus.Stale, $"could not recompute the unsevered baseline for '{word}': {baseline.Error}");
        if (baseline.Outcome != claim.Before)
        {
            return (
                DataflowClaimReviewStatus.Stale,
                $"current baseline outcome for '{word}' is '{baseline.Outcome}', not the claimed before '{claim.Before}'"
            );
        }

        foreach ((string element, string attribute, string role) in new[]
        {
            (row.WriterElement, row.WriterAttribute, "writer"),
            (row.ReaderElement, row.ReaderAttribute, "reader"),
        })
        {
            SeveranceRecomputation severed = evaluate(fixture, element, attribute, word);
            if (severed.Outcome is null)
            {
                return (
                    DataflowClaimReviewStatus.Stale,
                    $"could not recompute severing the {role} ({element}.{attribute}) for '{word}': {severed.Error}"
                );
            }
            if (severed.Outcome != claim.After)
            {
                return (
                    DataflowClaimReviewStatus.Stale,
                    $"severing the {role} ({element}.{attribute}) now gives '{severed.Outcome}' for '{word}', "
                        + $"not the claimed after '{claim.After}'"
                );
            }
        }

        return (
            DataflowClaimReviewStatus.Reviewed,
            $"recomputed: '{word}' is '{claim.Before}' unsevered and '{claim.After}' with either the writer "
                + "or the reader severed, matching the claim"
        );
    }

    /// <summary>
    /// Existence-plus-outcome-difference only (see this class's own doc comment for what is left):
    /// confirms <see cref="ClaimedCellEntry.DistinctFrom"/> names a real word in the same fixture whose
    /// <see cref="WordEntry.ExpectFail"/> differs from this word's. Does not (yet) confirm both words
    /// claim a cell of the same chain, nor that the grammar difference between them is exactly the one
    /// named condition.
    /// </summary>
    private static (bool?, string) EvaluateDistinctFrom(
        ClaimedCellEntry claim,
        WordEntry word,
        IReadOnlyDictionary<string, WordEntry> wordsByName
    )
    {
        if (claim.DistinctFrom.Length == 0)
            return (null, "");

        if (!wordsByName.TryGetValue(claim.DistinctFrom, out WordEntry? counterpart))
            return (false, $"distinct_from '{claim.DistinctFrom}' does not exist in this fixture");

        if (word.ExpectFail == counterpart.ExpectFail)
        {
            return (
                false,
                $"'{word.Word}' and distinct_from '{claim.DistinctFrom}' both have expect_fail={word.ExpectFail} -- "
                    + "independence not demonstrated"
            );
        }

        return (
            true,
            $"'{word.Word}' (expect_fail={word.ExpectFail}) and distinct_from '{claim.DistinctFrom}' "
                + $"(expect_fail={counterpart.ExpectFail}) differ"
        );
    }

    // Structured extraction from DataflowObligationLedger's fixed Evidence string shape (see its
    // FindPairedWitness/EvaluateMcDcCell doc comments) -- used ONLY for the non-fatal
    // UnclaimedSatisfiedCells report, never for anything that can fail a build, so a format drift here
    // degrades to "(unparsed)" rather than an exception.
    private static readonly Regex PairedWitnessPattern = new(
        @"flip '(?<word>[^']+)' from failed to successful parse in (?<fixture>\S+) \(",
        RegexOptions.Compiled
    );

    private static string ExtractWitnessWord(string evidence) =>
        PairedWitnessPattern.Match(evidence) is { Success: true } m ? m.Groups["word"].Value : "(unparsed)";

    private static string ExtractWitnessFixture(string evidence) =>
        PairedWitnessPattern.Match(evidence) is { Success: true } m ? m.Groups["fixture"].Value : "(unparsed)";
}
