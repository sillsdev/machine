#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

/// <summary>How one <see cref="CoverageItem"/> ended up resolved, or not, by the gate.</summary>
public enum CoverageResolution
{
    /// <summary>Resolved by evidence carrying a real counter-example.</summary>
    Evidenced,

    /// <summary>Resolved by a proof, with no evidence claimed for the same item.</summary>
    Proven,

    /// <summary>Neither evidence with a counter-example nor a proof claims this item. A real gap.</summary>
    Unresolved,

    /// <summary>Both evidence and a proof claim this item. The proof outlived its evidence; stale.</summary>
    Conflicting,

    /// <summary>
    /// A proof was supplied but failed recomputation: the check the gate re-runs for its kind did not
    /// come back with the result the proof claims. Never resolved as <see cref="Proven"/> -- a rejected
    /// proof is treated as absent, so the item still fails completeness.
    /// </summary>
    Rejected,
}

public sealed record CoverageResolutionResult(
    string ItemId,
    CoverageResolution Resolution,
    CounterexampleKind CounterexampleKind,
    string Detail
);

public sealed record CompletenessReport(
    IReadOnlyList<CoverageResolutionResult> Items,
    IReadOnlyDictionary<CounterexampleKind, int> EvidencedCountsByCounterexampleKind,
    IReadOnlyList<string> OrphanedEvidenceItemIds,
    IReadOnlyList<string> OrphanedProofItemIds,
    bool IsComplete
);

/// <summary>
/// Recomputes, per <see cref="CoverageItem"/>, whether it resolves to evidence with a counter-example,
/// a proof that re-verifies, both (stale), or neither (a gap). See docs/coverage-pipeline-design.md's
/// Completeness section. Never blends <see cref="CounterexampleKind.Word"/> and
/// <see cref="CounterexampleKind.LoadFailure"/> into one count -- they are different strengths of
/// evidence and the whole point of the kind is that they stop being summed.
/// </summary>
public static class CoverageCompletenessGate
{
    public static CompletenessReport Evaluate(
        IReadOnlyList<CoverageItem> items,
        IReadOnlyList<Evidence> evidence,
        IReadOnlyList<Proof> proofs,
        Func<string, XDocument>? loadGrammar = null
    )
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(evidence);
        ArgumentNullException.ThrowIfNull(proofs);

        var duplicateEvidenceItemIds = evidence
            .GroupBy(e => e.ItemId, StringComparer.Ordinal)
            .Where(group => group.Skip(1).Any())
            .Select(group => group.Key)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        if (duplicateEvidenceItemIds.Length > 0)
        {
            throw new ArgumentException(
                $"Evidence contains duplicate rows for item(s): {string.Join(", ", duplicateEvidenceItemIds)}",
                nameof(evidence)
            );
        }

        var itemById = items.ToDictionary(item => item.Id, StringComparer.Ordinal);
        var evidenceByItem = evidence
            .Where(evidenceRow =>
                IsCompletenessEvidence(evidenceRow)
                && itemById.TryGetValue(evidenceRow.ItemId, out CoverageItem? item)
                && string.Equals(evidenceRow.Fixture, item.Fixture, StringComparison.Ordinal)
            )
            .ToDictionary(e => e.ItemId, StringComparer.Ordinal);
        var invalidEvidenceDetails = evidence
            .Where(evidenceRow => itemById.ContainsKey(evidenceRow.ItemId))
            .GroupBy(evidenceRow => evidenceRow.ItemId, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Where(evidenceRow =>
                        !IsCompletenessEvidence(evidenceRow)
                        || !string.Equals(evidenceRow.Fixture, itemById[group.Key].Fixture, StringComparison.Ordinal)
                    )
                    .Select(evidenceRow => EvidenceDiagnostic(evidenceRow, itemById[group.Key]))
                    .FirstOrDefault(),
                StringComparer.Ordinal
            );
        var proofByItem = proofs.ToDictionary(p => p.ItemId, StringComparer.Ordinal);
        var itemIds = itemById.Keys.ToHashSet(StringComparer.Ordinal);

        var counts = new Dictionary<CounterexampleKind, int>
        {
            [CounterexampleKind.Word] = 0,
            [CounterexampleKind.LoadFailure] = 0,
        };

        var results = new List<CoverageResolutionResult>();
        foreach (CoverageItem item in items.OrderBy(item => item.Id, StringComparer.Ordinal))
        {
            bool hasEvidence = evidenceByItem.TryGetValue(item.Id, out Evidence? matchedEvidence);
            bool hasInvalidEvidence = invalidEvidenceDetails.TryGetValue(item.Id, out string? invalidEvidenceDetail)
                && invalidEvidenceDetail is not null;
            bool hasProof = proofByItem.TryGetValue(item.Id, out Proof? matchedProof);

            if (hasEvidence && hasProof)
            {
                results.Add(
                    new CoverageResolutionResult(
                        item.Id,
                        CoverageResolution.Conflicting,
                        matchedEvidence!.CounterexampleKind,
                        $"has both evidence ({matchedEvidence.CounterexampleKind}) and proof "
                            + $"({matchedProof!.Kind}); a proof that outlives its evidence is stale"
                    )
                );
                continue;
            }

            if (hasEvidence)
            {
                counts[matchedEvidence!.CounterexampleKind]++;
                results.Add(
                    new CoverageResolutionResult(
                        item.Id,
                        CoverageResolution.Evidenced,
                        matchedEvidence.CounterexampleKind,
                        $"{matchedEvidence.CounterexampleKind} counter-example: {matchedEvidence.Mutation}"
                    )
                );
                continue;
            }

            if (hasInvalidEvidence)
            {
                results.Add(
                    new CoverageResolutionResult(
                        item.Id,
                        CoverageResolution.Unresolved,
                        CounterexampleKind.None,
                        invalidEvidenceDetail!
                    )
                );
                continue;
            }

            if (hasProof)
            {
                string? rejection = RejectRecomputedOrderingProof(item, matchedProof!, loadGrammar);
                if (rejection is not null)
                {
                    results.Add(new CoverageResolutionResult(item.Id, CoverageResolution.Rejected, CounterexampleKind.None, rejection));
                    continue;
                }

                results.Add(
                    new CoverageResolutionResult(
                        item.Id,
                        CoverageResolution.Proven,
                        CounterexampleKind.None,
                        $"{matchedProof!.Kind}: {matchedProof.Check}"
                    )
                );
                continue;
            }

            results.Add(
                new CoverageResolutionResult(
                    item.Id,
                    CoverageResolution.Unresolved,
                    CounterexampleKind.None,
                    "neither evidence with a counter-example nor a proof claims this item"
                )
            );
        }

        string[] orphanedEvidence = evidence
            .Select(evidenceRow => evidenceRow.ItemId)
            .Where(id => !itemIds.Contains(id))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        string[] orphanedProofs = proofs
            .Select(proof => proof.ItemId)
            .Where(id => !itemIds.Contains(id))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();

        bool complete =
            orphanedEvidence.Length == 0
            &&
            orphanedProofs.Length == 0
            && results.All(result => result.Resolution is CoverageResolution.Evidenced or CoverageResolution.Proven);

        return new CompletenessReport(results, counts, orphanedEvidence, orphanedProofs, complete);
    }

    private static string EvidenceDiagnostic(Evidence evidence, CoverageItem item)
    {
        if (!string.Equals(evidence.Fixture, item.Fixture, StringComparison.Ordinal))
        {
            return $"evidence fixture '{evidence.Fixture}' does not match generated item fixture '{item.Fixture}'";
        }

        return "evidence claim is invalid or lacks a structural counter-example";
    }

    /// <summary>
    /// Returns whether an evidence record has the exact verdict/kind pairing and structural payload
    /// that can resolve an item: a word counter-example from an evidenced verdict, or a load failure
    /// from the required-to-load or jointly-evidenced verdict. Other pairings, including
    /// <see cref="CounterexampleKind.None"/>, are not completeness evidence.
    /// </summary>
    private static bool IsCompletenessEvidence(Evidence evidence) =>
        (evidence.Verdict, evidence.CounterexampleKind) is
                (CounterfactualVerdict.Evidenced, CounterexampleKind.Word)
                or (CounterfactualVerdict.EvidencedJointly, CounterexampleKind.Word)
                or (CounterfactualVerdict.EvidencedJointly, CounterexampleKind.LoadFailure)
                or (CounterfactualVerdict.RequiredToLoad, CounterexampleKind.LoadFailure)
        && !string.IsNullOrWhiteSpace(evidence.ExampleWord)
        && evidence.ExampleOutcome is not null
        && evidence.CounterexampleOutcome is not null
        && !string.Equals(evidence.ExampleOutcome, evidence.CounterexampleOutcome, StringComparison.Ordinal);

    /// <summary>
    /// Null only when <paramref name="proof"/>'s kind is one of the seven recomputed Ordering-proof kinds
    /// and it re-verifies against the current grammar. Unknown and surface-side kinds
    /// (<see cref="ImpossibilityProofs.DtdDefault"/> and siblings) fail closed until a mechanical verifier
    /// exists; checked-in prose never resolves completeness.
    /// </summary>
    private static string? RejectRecomputedOrderingProof(CoverageItem item, Proof proof, Func<string, XDocument>? loadGrammar)
    {
        bool isRecomputed =
            proof.Kind == OrderingProofs.Kind
            || proof.Kind == UnorderedInvariantProofs.Kind
            || proof.Kind == InactiveMemberProofs.Kind
            || proof.Kind == PosDisjointProofs.Kind
            || proof.Kind == TemplateMaskedProofs.Kind
            || proof.Kind == NeverFiresProofs.Kind
            || proof.Kind == FeatureValueDisjointProofs.Kind;
        if (!isRecomputed)
        {
            bool isKnownSurfaceKind =
                proof.Kind == ImpossibilityProofs.DtdDefault
                || proof.Kind == ImpossibilityProofs.NoConsumer
                || proof.Kind == ImpossibilityProofs.NotInSignature
                || proof.Kind == ImpossibilityProofs.BlockedByDefect;
            return isKnownSurfaceKind
                ? $"{proof.Kind} proof cannot be verified: surface proof kinds require a mechanical verifier"
                : $"Unknown proof kind '{proof.Kind}' cannot be verified: a mechanical verifier exists only for recomputed ordering proof kinds";
        }

        if (loadGrammar is null)
        {
            return $"{proof.Kind} proof cannot be verified: Evaluate was not given a grammar loader";
        }

        XDocument grammar = loadGrammar(item.Fixture);
        bool verified =
            proof.Kind == OrderingProofs.Kind ? OrderingProofs.Verify(grammar, item.Fixture, proof)
            : proof.Kind == UnorderedInvariantProofs.Kind ? UnorderedInvariantProofs.Verify(grammar, item.Fixture, proof)
            : proof.Kind == InactiveMemberProofs.Kind ? InactiveMemberProofs.Verify(grammar, item.Fixture, proof)
            : proof.Kind == PosDisjointProofs.Kind ? PosDisjointProofs.Verify(grammar, item.Fixture, proof)
            : proof.Kind == TemplateMaskedProofs.Kind ? TemplateMaskedProofs.Verify(grammar, item.Fixture, proof)
            : proof.Kind == NeverFiresProofs.Kind ? NeverFiresProofs.Verify(grammar, item.Fixture, proof)
            : FeatureValueDisjointProofs.Verify(grammar, item.Fixture, proof);
        return verified ? null : $"{proof.Kind} proof failed recomputation for '{proof.ItemId}' in '{item.Fixture}'";
    }
}
