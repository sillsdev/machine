using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class ConstructClaimCorroborationTests
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

    // 444 total claims matches conformance/coverage.csv's own row count (the same (fixture, word,
    // signature, construct) enumeration CoverageReport.WriteCsvs already produces). Of those, 185 map
    // to a real DTD identifier this fixture's grammar.xml actually contains (Confirmed), 244 name a
    // construct whose text ConstructClaimCorroboration.MapConstructsToDtdTokens cannot mechanically
    // resolve to any DTD identifier at all (Unmapped -- most constructs are prose, not identifiers,
    // so this is the expected majority, not a defect), and 15 are Contradicted. (Was 441/185/241/15
    // before the coverage-cell word `idil` (metathesis-phase-isolation) added one claimed-construct
    // row for its PartOfSpeech::LexicalEntry.partOfSpeech->PhonologicalSubrule.requiredPartsOfSpeech
    // AbsentGatedForm witness; 442 -> 444 when author-coverage-cell added `gofz`/`ygofz` to
    // languages/fusional-realizational-morphology, each claiming the prose construct "MPR
    // features/groups" -- both Unmapped, for `ygofz`'s MorphologicalOutput.MPRFeatures->
    // MorphologicalInput.requiredMPRFeatures AbsentGatedForm witness. 452 -> 464 when
    // rewrite-analysis-feature-neutralization/synthesis-stratum-render-stale-table were added.
    // 464 -> 475 when cross-table-root-respelling was added (7 rows) and the new "CharacterDefinitionTable:
    // cross-table respelling" construct was claimed by four parses across three fixtures; all eleven
    // corroborate on the CharacterDefinitionTable token, so confirmed rises by the same eleven.
    // 210/250 -> 213/247 (rows unchanged at 475) when edge-cases/feature-gating-breadth's rrPast
    // converted from RealizationalRule to an ordinary MorphologicalRule: kalid/kalmuid/kalidmu's
    // RealizationalAffixProcessRule claims (prose, Unmapped) became "Syntactic feature agreement (...)"
    // claims instead, which corroborate on OutputHeadFeatures (Confirmed) -- three rows move status,
    // none are added or removed.
    //
    // 475/213/15/247 -> 473/211/12/250 after edge-cases/morphotactic-attribute-breadth's and
    // languages/fusional-realizational-morphology's family-blocking fieldworks_producible conversions
    // (this branch): the fixture's removed words (bak/dom/bakgi/sim/rog/simru/kulgi/kulru/kulsi/sol/
    // kulbubidu/kulgimo/kulmoru -- family+blockable, RealizationalRule, the require-type co-occurrence
    // rule, the append-mode MPR group, and their Slot-ordering discriminators) took 2 Confirmed and 3
    // Contradicted claims with them (all 3 Contradicted were among the 15 pinned by
    // AllContradictedClaimsTraceToTheKnownBundledConstructLimitation, which is why that count also
    // drops, 15 -> 12); its new/kept words (topdori/topdo/topri/kulge/topko/topla/topne/toplane, plus
    // the words that already existed) reclaim coverage.csv rows the conversion had silently dropped
    // (see the fixture's own words.yaml "COVERAGE.CSV REGRESSION" note) at a net of -2 rows overall.
    [Test]
    public void CheckedInLedgerHasTheMeasuredClaimAndStatusCounts()
    {
        string root = RepositoryRoot();
        IReadOnlyList<ConstructClaimCorroboration.Row> rows = ConstructClaimCorroboration.Read(root);

        int confirmed = rows.Count(r => r.Status == ConstructClaimStatus.Confirmed);
        int contradicted = rows.Count(r => r.Status == ConstructClaimStatus.Contradicted);
        int unmapped = rows.Count(r => r.Status == ConstructClaimStatus.Unmapped);

        TestContext.Out.WriteLine(
            $"rows={rows.Count} confirmed={confirmed} contradicted={contradicted} unmapped={unmapped}"
        );

        Assert.That(rows, Has.Count.EqualTo(473));
        Assert.That(confirmed, Is.EqualTo(211));
        Assert.That(contradicted, Is.EqualTo(12));
        Assert.That(unmapped, Is.EqualTo(250));
    }

    // All 12 Contradicted claims trace to one (fixture, construct) pair: edge-cases/morphotactic-
    // attribute-breadth claiming the bundled construct "Ordinary/realizational rule constraints
    // (MaxApplicationCount/RequiredStemName/Blockable)" (was 15, before this fixture's own
    // fieldworks_producible conversion (this branch) removed its family+blockable material -- 3 of the
    // 15 claims went with the removed words; see CheckedInLedgerHasTheMeasuredClaimAndStatusCounts's own
    // comment). Manually verified (see the task report) that this is a corroboration-heuristic
    // limitation, not a false claim by the fixture author: the fixture genuinely exercises
    // multipleApplication and (pre-conversion) blockable, but "MaxApplicationCount" names no real DTD
    // identifier (the real attribute is multipleApplication) and "Blockable" is filtered out by the
    // internal-capital heuristic (a bare English word) -- so the only token this mapping could ever
    // check for this construct is requiredStemName, which this fixture indeed never uses. Pinned so a
    // change here is investigated, not silently re-baselined.
    [Test]
    public void AllContradictedClaimsTraceToTheKnownBundledConstructLimitation()
    {
        string root = RepositoryRoot();
        IReadOnlyList<ConstructClaimCorroboration.Row> rows = ConstructClaimCorroboration.Read(root);
        ConstructClaimCorroboration.Row[] contradicted = rows.Where(r => r.Status == ConstructClaimStatus.Contradicted)
            .ToArray();

        Assert.That(contradicted, Has.Length.EqualTo(12));
        Assert.That(
            contradicted.Select(r => r.Fixture).Distinct(),
            Is.EquivalentTo(new[] { "edge-cases/morphotactic-attribute-breadth" })
        );
        Assert.That(
            contradicted.Select(r => r.Construct).Distinct(),
            Is.EquivalentTo(
                new[] { "Ordinary/realizational rule constraints (MaxApplicationCount/RequiredStemName/Blockable)" }
            )
        );
        Assert.That(contradicted.Select(r => r.MatchedTokens.Count), Is.All.EqualTo(1));
    }

    // Cheap: constructs.txt, the DTD (once), and every fixture's already-loaded grammar.xml/words.yaml
    // -- no reparse, so this runs on every pass rather than needing [Explicit].
    [Test]
    public void CheckedInConstructClaimCorroborationIsUpToDate()
    {
        string root = RepositoryRoot();
        string constructsPath = Path.Combine(root, "conformance", "constructs.txt");
        IReadOnlyList<ConstructClaimCorroboration.Row> fresh = ConstructClaimCorroboration.Compute(
            root,
            constructsPath
        );
        string freshText = ConstructClaimCorroboration.ToText(fresh);
        string checkedIn = File.ReadAllText(
            Path.Combine(root, ConstructClaimCorroboration.RelativePath.Replace('/', Path.DirectorySeparatorChar))
        );

        Assert.That(
            freshText.ReplaceLineEndings("\n"),
            Is.EqualTo(checkedIn.ReplaceLineEndings("\n")),
            "regenerate with: hc-conformance --write-coverage-traceability --repository-root ."
        );
    }
}
