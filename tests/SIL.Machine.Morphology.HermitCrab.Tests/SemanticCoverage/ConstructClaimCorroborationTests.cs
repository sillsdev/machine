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
    // MorphologicalInput.requiredMPRFeatures AbsentGatedForm witness.)
    [Test]
    public void CheckedInLedgerHasTheMeasuredClaimAndStatusCounts()
    {
        string root = RepositoryRoot();
        IReadOnlyList<ConstructClaimCorroboration.Row> rows = ConstructClaimCorroboration.Read(root);

        int confirmed = rows.Count(r => r.Status == ConstructClaimStatus.Confirmed);
        int contradicted = rows.Count(r => r.Status == ConstructClaimStatus.Contradicted);
        int unmapped = rows.Count(r => r.Status == ConstructClaimStatus.Unmapped);

        TestContext.Out.WriteLine($"rows={rows.Count} confirmed={confirmed} contradicted={contradicted} unmapped={unmapped}");

        Assert.That(rows, Has.Count.EqualTo(445));
        Assert.That(confirmed, Is.EqualTo(186));
        Assert.That(contradicted, Is.EqualTo(15));
        Assert.That(unmapped, Is.EqualTo(244));
    }

    // All 15 Contradicted claims trace to one (fixture, construct) pair: edge-cases/morphotactic-
    // attribute-breadth claiming the bundled construct "Ordinary/realizational rule constraints
    // (MaxApplicationCount/RequiredStemName/Blockable)". Manually verified (see the task report) that
    // this is a corroboration-heuristic limitation, not a false claim by the fixture author: the
    // fixture genuinely exercises multipleApplication and blockable (2 of the 3 bundled things), but
    // "MaxApplicationCount" names no real DTD identifier (the real attribute is multipleApplication)
    // and "Blockable" is filtered out by the internal-capital heuristic (a bare English word) -- so the
    // only token this mapping could ever check for this construct is requiredStemName, which this
    // fixture indeed never uses. Pinned so a change here is investigated, not silently re-baselined.
    [Test]
    public void AllContradictedClaimsTraceToTheKnownBundledConstructLimitation()
    {
        string root = RepositoryRoot();
        IReadOnlyList<ConstructClaimCorroboration.Row> rows = ConstructClaimCorroboration.Read(root);
        ConstructClaimCorroboration.Row[] contradicted = rows.Where(r => r.Status == ConstructClaimStatus.Contradicted).ToArray();

        Assert.That(contradicted, Has.Length.EqualTo(15));
        Assert.That(contradicted.Select(r => r.Fixture).Distinct(), Is.EquivalentTo(new[] { "edge-cases/morphotactic-attribute-breadth" }));
        Assert.That(
            contradicted.Select(r => r.Construct).Distinct(),
            Is.EquivalentTo(new[] { "Ordinary/realizational rule constraints (MaxApplicationCount/RequiredStemName/Blockable)" })
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
        IReadOnlyList<ConstructClaimCorroboration.Row> fresh = ConstructClaimCorroboration.Compute(root, constructsPath);
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
