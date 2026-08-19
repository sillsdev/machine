using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class DataflowClaimGateTests
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

    private const string SatisfiedCellId =
        "MorphologicalPhonologicalRuleFeature::LexicalEntry.ruleFeatures->MorphologicalInput.excludedMPRFeatures::McDc:PresentGatedForm";

    private static DataflowObligationLedger.Row SatisfiedRow(string cellId = SatisfiedCellId) =>
        new(
            cellId,
            "LexicalEntry",
            "ruleFeatures",
            "MorphologicalPhonologicalRuleFeature",
            "MorphologicalInput",
            "excludedMPRFeatures",
            "McDc",
            "PresentGatedForm",
            "-",
            ObligationStatus.Satisfied,
            "paired witness: severing writer and reader both flip 'vokadan' from failed to successful "
                + "parse in edge-cases/mpr-gated-exception (conformance/interface-witness.tsv)"
        );

    private static DataflowObligationLedger.Row UnknownStatusRow(string cellId) =>
        new(cellId, "A", "a", "P", "B", "b", "McDc", "AbsentControl", "-", ObligationStatus.Unknown, "no witness yet");

    private static WordEntry PlainWord(string word, bool expectFail = false) => new() { Word = word, Note = "test fixture", ExpectFail = expectFail };

    private static WordEntry WordWithClaims(string word, params ClaimedCellEntry[] claims)
    {
        var entry = new WordEntry { Word = word, Note = "test fixture" };
        entry.ClaimedCells.AddRange(claims);
        return entry;
    }

    private static WordEntry WordWithClaims(string word, bool expectFail, params ClaimedCellEntry[] claims)
    {
        var entry = new WordEntry { Word = word, Note = "test fixture", ExpectFail = expectFail };
        entry.ClaimedCells.AddRange(claims);
        return entry;
    }

    private static Fixture FixtureWith(string id, params WordEntry[] words)
    {
        var yaml = new WordsYaml { Language = "Test" };
        yaml.Words.AddRange(words);
        return new Fixture(id, "unused-directory", yaml);
    }

    private static ClaimedCellEntry ReviewedClaim(string cell, string before, string after) =>
        new()
        {
            Cell = cell,
            Severing = "test severing description",
            Before = before,
            After = after,
            Proof = "test proof",
        };

    // Falsification #1: a claimed cell id that does not appear anywhere in dataflow-obligations.tsv
    // must fail -- catches a typo or an id orphaned when a chain's schema shape changed.
    [Test]
    public void ClaimOnNonexistentCellIdFails()
    {
        var ledger = new[] { SatisfiedRow() };
        var fixture = FixtureWith("edge-cases/fake", WordWithClaims("w1", new ClaimedCellEntry { Cell = "totally-fake-cell-id" }));

        DataflowClaimReport report = DataflowClaimGate.Evaluate(new[] { fixture }, ledger, ThrowingRecompute);

        Assert.That(report.AllClaimsValid, Is.False);
        DataflowClaimResult result = report.Claims.Single();
        Assert.That(result.Validity, Is.EqualTo(DataflowClaimValidity.UnknownCellId));
    }

    // Falsification #2: a claim on a cell id that DOES exist but whose ledger status is not Satisfied
    // must fail -- the author intended a witness and did not get one.
    [Test]
    public void ClaimOnNotSatisfiedCellFails()
    {
        var ledger = new[] { UnknownStatusRow("cell-x") };
        var fixture = FixtureWith("edge-cases/fake", WordWithClaims("w1", new ClaimedCellEntry { Cell = "cell-x" }));

        DataflowClaimReport report = DataflowClaimGate.Evaluate(new[] { fixture }, ledger, ThrowingRecompute);

        Assert.That(report.AllClaimsValid, Is.False);
        DataflowClaimResult result = report.Claims.Single();
        Assert.That(result.Validity, Is.EqualTo(DataflowClaimValidity.NotSatisfied));
    }

    // A claim on a cell id that exists AND is Satisfied passes, with no review bundle recorded --
    // recompute is never even called, since there is nothing to recompute.
    [Test]
    public void ClaimOnSatisfiedCellPassesUnreviewedWithNoReviewBundle()
    {
        var ledger = new[] { SatisfiedRow("cell-ok") };
        var fixture = FixtureWith("edge-cases/mpr-gated-exception", WordWithClaims("vokadan", new ClaimedCellEntry { Cell = "cell-ok" }));

        DataflowClaimReport report = DataflowClaimGate.Evaluate(new[] { fixture }, ledger, ThrowingRecompute);

        Assert.That(report.AllClaimsValid, Is.True);
        DataflowClaimResult result = report.Claims.Single();
        Assert.That(result.Validity, Is.EqualTo(DataflowClaimValidity.Valid));
        Assert.That(result.Review, Is.EqualTo(DataflowClaimReviewStatus.Unreviewed));
    }

    // A review bundle whose before/after matches what recomputation produces (for the unsevered
    // baseline, the writer severed, and the reader severed) is Reviewed.
    [Test]
    public void ReviewBundleMatchingRecomputationIsReviewed()
    {
        var ledger = new[] { SatisfiedRow("cell-ok") };
        var claim = ReviewedClaim("cell-ok", before: "ok::-", after: "ok::VOKAD+SUF|vokadan");
        var fixture = FixtureWith("edge-cases/mpr-gated-exception", WordWithClaims("vokadan", claim));

        RecomputeSeverance recompute = (f, element, attribute, word) =>
            new SeveranceRecomputation(element is null ? "ok::-" : "ok::VOKAD+SUF|vokadan", null);

        DataflowClaimReport report = DataflowClaimGate.Evaluate(new[] { fixture }, ledger, recompute);

        Assert.That(report.AllClaimsValid, Is.True);
        Assert.That(report.Claims.Single().Review, Is.EqualTo(DataflowClaimReviewStatus.Reviewed));
    }

    // A review bundle whose after no longer matches recomputation (the evidence moved) is Stale --
    // reported, but the claim itself still passes (staleness never fails the build by itself).
    [Test]
    public void ReviewBundleDisagreeingWithRecomputationIsStaleNotFailed()
    {
        var ledger = new[] { SatisfiedRow("cell-ok") };
        var claim = ReviewedClaim("cell-ok", before: "ok::-", after: "ok::VOKAD+SUF|vokadan");
        var fixture = FixtureWith("edge-cases/mpr-gated-exception", WordWithClaims("vokadan", claim));

        // The writer-severed recomputation now produces something different from what was claimed --
        // e.g. the fixture grew a second entry so the mutant's outcome changed.
        RecomputeSeverance recompute = (f, element, attribute, word) =>
            element == "LexicalEntry"
                ? new SeveranceRecomputation("ok::VOKAD+SUF|vokadan-different", null)
                : new SeveranceRecomputation(element is null ? "ok::-" : "ok::VOKAD+SUF|vokadan", null);

        DataflowClaimReport report = DataflowClaimGate.Evaluate(new[] { fixture }, ledger, recompute);

        Assert.That(report.AllClaimsValid, Is.True, "staleness must never fail the build by itself");
        Assert.That(report.Claims.Single().Review, Is.EqualTo(DataflowClaimReviewStatus.Stale));
    }

    // A review bundle whose recomputation cannot even be attempted (e.g. the attribute no longer
    // occurs in the fixture to sever) is Stale, never silently Reviewed.
    [Test]
    public void ReviewBundleThatCannotBeRecomputedIsStaleNotReviewed()
    {
        var ledger = new[] { SatisfiedRow("cell-ok") };
        var claim = ReviewedClaim("cell-ok", before: "ok::-", after: "ok::VOKAD+SUF|vokadan");
        var fixture = FixtureWith("edge-cases/mpr-gated-exception", WordWithClaims("vokadan", claim));

        RecomputeSeverance recompute = (f, element, attribute, word) =>
            element == "MorphologicalInput"
                ? new SeveranceRecomputation(null, "attribute no longer occurs")
                : new SeveranceRecomputation(element is null ? "ok::-" : "ok::VOKAD+SUF|vokadan", null);

        DataflowClaimReport report = DataflowClaimGate.Evaluate(new[] { fixture }, ledger, recompute);

        Assert.That(report.AllClaimsValid, Is.True);
        Assert.That(report.Claims.Single().Review, Is.EqualTo(DataflowClaimReviewStatus.Stale));
    }

    // distinct_from, existence-plus-outcome-difference: the named word exists in the same fixture and
    // its expect_fail outcome differs from the claiming word's.
    [Test]
    public void DistinctFromExistingWordWithDifferentOutcomeIsVerified()
    {
        var ledger = new[] { SatisfiedRow("cell-ok") };
        var claim = new ClaimedCellEntry { Cell = "cell-ok", DistinctFrom = "control" };
        var fixture = FixtureWith(
            "edge-cases/mpr-gated-exception",
            WordWithClaims("vokadan", expectFail: true, claim),
            PlainWord("control", expectFail: false)
        );

        DataflowClaimReport report = DataflowClaimGate.Evaluate(new[] { fixture }, ledger, ThrowingRecompute);

        DataflowClaimResult result = report.Claims.Single();
        Assert.That(result.DistinctFromVerified, Is.True);
    }

    // distinct_from naming a word that does not exist in the fixture is reported unverified, never
    // silently accepted.
    [Test]
    public void DistinctFromMissingWordIsUnverified()
    {
        var ledger = new[] { SatisfiedRow("cell-ok") };
        var claim = new ClaimedCellEntry { Cell = "cell-ok", DistinctFrom = "nonexistent" };
        var fixture = FixtureWith("edge-cases/mpr-gated-exception", WordWithClaims("vokadan", expectFail: true, claim));

        DataflowClaimReport report = DataflowClaimGate.Evaluate(new[] { fixture }, ledger, ThrowingRecompute);

        Assert.That(report.Claims.Single().DistinctFromVerified, Is.False);
    }

    // distinct_from naming a word whose outcome does NOT differ (both fail, or both pass) fails to
    // demonstrate independence and is reported unverified.
    [Test]
    public void DistinctFromWithSameOutcomeIsUnverified()
    {
        var ledger = new[] { SatisfiedRow("cell-ok") };
        var claim = new ClaimedCellEntry { Cell = "cell-ok", DistinctFrom = "alsofails" };
        var fixture = FixtureWith(
            "edge-cases/mpr-gated-exception",
            WordWithClaims("vokadan", expectFail: true, claim),
            PlainWord("alsofails", expectFail: true)
        );

        DataflowClaimReport report = DataflowClaimGate.Evaluate(new[] { fixture }, ledger, ThrowingRecompute);

        Assert.That(report.Claims.Single().DistinctFromVerified, Is.False);
    }

    // The reverse direction (docs/dataflow-coverage-plan.md): a Satisfied cell no word claims is
    // reported, never failed. Uses a throwing recompute to also confirm recomputation is never invoked
    // when there is nothing claimed.
    [Test]
    public void UnclaimedSatisfiedCellIsReportedNotFailed()
    {
        var ledger = new[] { SatisfiedRow("cell-ok") };
        var fixture = FixtureWith("edge-cases/mpr-gated-exception", WordWithClaims("someOtherWord"));

        DataflowClaimReport report = DataflowClaimGate.Evaluate(new[] { fixture }, ledger, ThrowingRecompute);

        Assert.That(report.AllClaimsValid, Is.True, "an unclaimed Satisfied cell must never fail the build");
        Assert.That(report.UnclaimedSatisfiedCells, Has.Count.EqualTo(1));
        Assert.That(report.UnclaimedSatisfiedCells.Single().WitnessWord, Is.EqualTo("vokadan"));
    }

    private static SeveranceRecomputation ThrowingRecompute(Fixture fixture, string? element, string? attribute, string word) =>
        throw new InvalidOperationException("recompute should not be invoked when there is no review bundle to check");

    // Falsification #3, on the real corpus, with the REAL severance recomputation (DefaultRecompute):
    // the two seeded claims (edge-cases/mpr-gated-exception's 'vokadan' and
    // languages/polysynthetic-stratal-derivation-chain's 'nunavuq') must validate, their inline
    // before/after must actually recompute as Reviewed against the real grammars, and their
    // distinct_from words must verify. Deliberately does not pin the total Satisfied-cell count or the
    // unclaimed count -- both are expected to move as the concurrent mutator-class work and future
    // authoring land (see this task's own "tolerate the ledger gaining rows" instruction).
    [Test]
    public void SeededRealClaimsValidateAndReviewAgainstTheRealCorpus()
    {
        string root = RepositoryRoot();
        IReadOnlyList<Fixture> fixtures = Fixture.DiscoverAll(Path.Combine(root, "conformance"));
        IReadOnlyList<DataflowObligationLedger.Row> ledger = DataflowObligationLedger.Read(root);

        DataflowClaimReport report = DataflowClaimGate.Evaluate(fixtures, ledger, DataflowClaimGate.DefaultRecompute);

        Assert.That(
            report.AllClaimsValid,
            Is.True,
            string.Join(
                "\n",
                report.Claims.Where(c => c.Validity != DataflowClaimValidity.Valid)
                    .Select(c => $"{c.FixtureId}/{c.Word}: {c.CellId}: {c.Detail}")
            )
        );

        (string FixtureId, string Word, string CellId)[] seeded =
        {
            ("edge-cases/mpr-gated-exception", "vokadan", SatisfiedCellId),
            (
                "languages/polysynthetic-stratal-derivation-chain",
                "nunavuq",
                "PartOfSpeech::LexicalEntry.partOfSpeech->MorphologicalRule.requiredPartsOfSpeech::McDc:AbsentGatedForm"
            ),
        };
        foreach ((string fixtureId, string word, string cellId) in seeded)
        {
            DataflowClaimResult match = report.Claims.Single(c =>
                c.FixtureId == fixtureId && c.Word == word && c.CellId == cellId
            );
            Assert.That(match.Validity, Is.EqualTo(DataflowClaimValidity.Valid), $"{fixtureId}/{word}");
            Assert.That(match.Review, Is.EqualTo(DataflowClaimReviewStatus.Reviewed), $"{fixtureId}/{word}: {match.ReviewDetail}");
            Assert.That(match.DistinctFromVerified, Is.True, $"{fixtureId}/{word}: {match.DistinctFromDetail}");
        }

        TestContext.Out.WriteLine($"total claims={report.Claims.Count} unclaimed satisfied cells={report.UnclaimedSatisfiedCells.Count}");
        foreach (UnclaimedSatisfiedCell cell in report.UnclaimedSatisfiedCells)
            TestContext.Out.WriteLine($"  unclaimed: {cell.CellId} (witness: '{cell.WitnessWord}' in {cell.FixtureId})");
    }
}
