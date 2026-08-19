using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// Pins the corpus-wide gap count -- items neither evidenced (conformance/semantic-coverage-evidence.tsv)
/// nor proven (conformance/semantic-coverage-proofs.tsv plus a fresh, cheap disjoint-domains/unordered-
/// invariant/inactive-member/pos-disjoint recompute for Ordering) -- without recomputing the expensive
/// sweep: every input here is either a checked-in file or a pure, engine-free grammar read. Gaps may only
/// go down. A gate that fails on every existing gap would block all ordinary work and get switched off, so
/// this is a ratchet: it fails only if the count goes UP.
/// </summary>
[TestFixture]
public sealed class CoverageGapRatchetTests
{
    private static string RepositoryRoot()
    {
        string? directory = TestContext.CurrentContext.TestDirectory;
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory, "conformance", "constructs.txt")))
                return directory;
            directory = Directory.GetParent(directory)?.FullName;
        }

        Assert.Fail("Could not locate the repository root.");
        return string.Empty;
    }

    // From a real corpus-wide run: 194 Surface items with 191 evidenced + 3 rejected prose proofs; 138
    // Ordering items with 32 evidenced + 88 recomputed proven + 18 gap. Lower this pin whenever a fresh
    // --write-coverage-evidence run finds fewer gaps. Never raise it without explaining why coverage
    // regressed.
    //
    // Raised 31 -> 32: disjoint-domains now also checks Environment/LeftEnvironment/RightEnvironment
    // (feeding/bleeding), not only PhoneticOutput vs PhoneticInput, which un-certifies 4 phonological
    // pairs that were never actually independent. None of the four has evidence yet, so each is now an
    // honest gap. A fifth un-certified pair has real empirical evidence already in the ledger, so it was
    // never a gap either way -- see OrderingGeneratorTests for that pair by name.
    //
    // Lowered 32 -> 29: the new template-masked proof kind certifies 3 AffixTemplateSlots pairs whose
    // owning Stratum is unordered and whose template's rules are all in the cascade (person~number and
    // number~evidential in suffixing-evidential-adjacency-chain; tense~number in suffixing-vowel-harmony).
    //
    // Lowered 26 -> 24: inactive-member now also certifies an AffixTemplateSlots member that is itself
    // active but whose morphologicalRules all fail to resolve to an active rule (an empty Slot), not
    // only a member carrying isActive="no" itself. Certifies realSlot~realDecoySlot and
    // realDecoySlot~coASlot in edge-cases/morphotactic-attribute-breadth, both empty because their only
    // rule mrRealDecoy is isActive="no".
    //
    // Lowered 24 -> 22: two new recomputed kinds. never-fires certifies edge-cases/feature-system-breadth's
    // prHighTrigger~mtSwap (prHighTrigger's LeftEnvironment names ncHighCons, which resolves to zero active
    // segments -- no active segment is both hiPlus and hiCons). feature-value-disjoint certifies
    // edge-cases/mpr-gated-exception's prNasalAssimBilabial~prNasalAssimAlveolar (disjoint featPlace
    // environment classes, neither output reachable from the other's environment or input class). A third
    // candidate, edge-cases/right-to-left-anchor-environment's prRtlAnchor~prSpread, was investigated and
    // rejected: the two rules' Environments genuinely differ (a word-final RightEnvironment anchor vs. a
    // LeftEnvironment natural class), so it does not meet this kind's own "environments must not
    // differentiate them" bar even though input class and output segment happen to coincide -- left an
    // open gap rather than shipped as a proof.
    //
    // Lowered 22 -> 18: pos-disjoint now distinguishes a same-side POS-preserving MorphologicalRule
    // from a bridge that crosses between the two compared required-POS sets. This certifies four
    // suffixing-extension-slot-ordering pairs that were blocked only by their own or a sibling rule's
    // trivial posVn -> posVn output; cross-side, unrestricted, and CompoundingRule bridges still fail
    // closed.
    //
    // Raised 21 -> 23 (Surface still 3; Ordering 18 -> 20): f8199d69 ("Take the four fixtures
    // conformance-framework added, without its harness") added edge-cases/{free-fluctuating-
    // allomorph-pair, mpr-group-overwrite-without-realizational, process-morphology-in-place-mutation,
    // stem-name-restricted-root-allomorph}. Three of the four contribute zero Ordering gaps -- every
    // adjacent pair they declare is already evidenced or proven. The fourth,
    // mpr-group-overwrite-without-realizational, contributes exactly two:
    //   ordering:edge-cases/mpr-group-overwrite-without-realizational/morphologicalRules/mrThemeA~mrThemeB
    //   ordering:edge-cases/mpr-group-overwrite-without-realizational/morphologicalRules/mrThemeB~mrEndC
    // Both are genuinely order-sensitive (that is this fixture's entire point: mrThemeB's Overwrite
    // MPR-group semantics drop mprA, so mrEndC's requiredMPRFeatures="mprA" gate only passes before
    // mrThemeB runs -- see the fixture's own wudofq row, expect_fail: true). None of the seven
    // recomputed Ordering proof kinds model MPR-feature accumulation at all (pos-disjoint comes
    // closest but correctly reports Overlaps, not Disjoint, since all three rules share
    // requiredPartsOfSpeech="posN" -- POS alone cannot show these are independent, and they are not).
    // So neither pair can ever be closed by a *proof*; only real evidence from
    // --write-coverage-evidence (a checked-in row in conformance/semantic-coverage-evidence.tsv) can
    // resolve them, and generating that evidence is out of scope here -- semantic-coverage-evidence.tsv
    // is a shared, concurrently-owned artifact this fix does not touch. Recorded here by name, not
    // merely by count, so lowering this pin later means finding these two exact ids evidenced, not
    // just watching the total drop.
    private const int PinnedGapCount = 14;

    [Test]
    public void CorpusWideGapCountNeverIncreasesFromThePinnedValue()
    {
        string root = RepositoryRoot();

        // The evidenced set: read, never recomputed -- this is what makes the test cheap.
        IReadOnlyList<EvidenceLedger.Row> evidenceLedger = EvidenceLedger.Read(root);
        var evidencedIds = evidenceLedger.Select(row => row.ItemId).ToHashSet(StringComparer.Ordinal);

        // The full inventory, built structurally with no engine sweep: 194 Surface ids from the checked-in
        // Surface ledger (itself produced by a real sweep, but reading it back costs one file parse) plus
        // 138 Ordering ids enumerated fresh from the checked-in grammars.
        IReadOnlyList<CounterfactualResult> surfaceResults = CounterfactualLedger.Read(root);
        IReadOnlyList<CoverageItem> orderingItems = CoverageEvidencePipeline.BuildOrderingItems(root);

        Assert.That(surfaceResults, Is.Not.Empty, "run --write-counterfactual first to populate the Surface ledger");
        Assert.That(orderingItems, Is.Not.Empty, "the corpus must declare at least one Ordering item");

        IReadOnlyList<CoverageItem> items = CoverageEvidencePipeline.BuildItems(surfaceResults, Array.Empty<CounterfactualResult>())
            .Concat(orderingItems)
            .OrderBy(item => item.Id, StringComparer.Ordinal)
            .ToArray();
        var itemsById = items.ToDictionary(item => item.Id, StringComparer.Ordinal);
        string[] orphanedEvidenceRows = evidenceLedger
            .Select(row => row.ItemId)
            .Where(id => !itemsById.ContainsKey(id))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        Assert.That(orphanedEvidenceRows, Is.Empty, "evidence rows outside the generated inventory must fail independently of the gap pin");
        IReadOnlyList<Evidence> evidence = evidenceLedger
            .Select(row => EvidenceLedger.ToEvidence(row, itemsById[row.ItemId]))
            .ToArray();
        CoverageItem[] nonEvidencedOrdering = orderingItems.Where(item => !evidencedIds.Contains(item.Id)).ToArray();
        IReadOnlyList<Proof> proofs = CoverageEvidencePipeline.BuildProofs(root, nonEvidencedOrdering);
        CompletenessReport completeness = CoverageCompletenessGate.Evaluate(
            items,
            evidence,
            proofs,
            CoverageEvidencePipeline.GrammarLoader(root)
        );
        Assert.That(
            completeness.OrphanedEvidenceItemIds,
            Is.Empty,
            "evidence outside the generated inventory must fail independently of the gap pin"
        );
        Assert.That(
            completeness.Items.Where(result => result.Resolution == CoverageResolution.Conflicting),
            Is.Empty,
            "evidence/proof conflicts are stale claims, not gaps the numeric ratchet may absorb"
        );
        Assert.That(
            completeness.OrphanedProofItemIds,
            Is.Empty,
            "proofs outside the generated inventory must fail independently of the gap pin"
        );
        var kindByItemId = items.ToDictionary(item => item.Id, item => item.Kind, StringComparer.Ordinal);
        int surfaceGaps = completeness.Items.Count(result =>
            result.Resolution is CoverageResolution.Unresolved or CoverageResolution.Rejected
            && kindByItemId[result.ItemId] == CoverageItemKind.Surface
        );
        int orderingGaps = completeness.Items.Count(result =>
            result.Resolution is CoverageResolution.Unresolved or CoverageResolution.Rejected
            && kindByItemId[result.ItemId] == CoverageItemKind.Ordering
        );
        int totalGaps = surfaceGaps + orderingGaps;

        TestContext.Out.WriteLine(
            $"total items: {surfaceResults.Count + orderingItems.Count} "
                + $"(Surface {surfaceResults.Count}, Ordering {orderingItems.Count})"
        );
        TestContext.Out.WriteLine($"gaps: {totalGaps} (Surface {surfaceGaps}, Ordering {orderingGaps}); pinned at {PinnedGapCount}");
        foreach (var group in completeness.Items.Where(result => result.Resolution == CoverageResolution.Proven).GroupBy(result => result.Detail.Split(':')[0]).OrderByDescending(group => group.Count()))
            TestContext.Out.WriteLine($"  proven ({group.Key}): {group.Count()}");
        TestContext.Out.WriteLine($"  rejected: {completeness.Items.Count(result => result.Resolution == CoverageResolution.Rejected)}");
        TestContext.Out.WriteLine($"  unresolved: {completeness.Items.Count(result => result.Resolution == CoverageResolution.Unresolved)}");

        Assert.That(
            totalGaps,
            Is.LessThanOrEqualTo(PinnedGapCount),
            $"corpus-wide gaps increased from the pinned {PinnedGapCount} to {totalGaps}; coverage regressed"
        );
        if (totalGaps < PinnedGapCount)
        {
            Assert.Warn(
                $"corpus-wide gaps decreased from the pinned {PinnedGapCount} to {totalGaps}; "
                    + $"lower CoverageGapRatchetTests.PinnedGapCount to {totalGaps}."
            );
        }
    }
}
