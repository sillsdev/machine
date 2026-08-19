using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class EvidenceLedgerTests
{
    private string _repositoryRoot = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _repositoryRoot = Path.Combine(Path.GetTempPath(), "hc-evidence-ledger-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_repositoryRoot, "conformance"));
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_repositoryRoot))
            Directory.Delete(_repositoryRoot, recursive: true);
    }

    [Test]
    public void WritingThenReadingReturnsIdenticalRecords()
    {
        var rows = new[]
        {
            new EvidenceLedger.Row(
                "dtd:element/AffixTemplates",
                CoverageItemKind.Surface,
                "edge-cases/loader-isactive-breadth",
                "takul",
                "ok::TA+KUL|takul",
                CounterexampleKind.Word,
                "ok::-",
                "removed 1 <AffixTemplates> element(s)",
                CounterfactualVerdict.Evidenced
            ),
            new EvidenceLedger.Row(
                "dtd:element/AffixTemplate",
                CoverageItemKind.Surface,
                "edge-cases/diacritic-segments",
                "some-word",
                "ok::SIGNATURE",
                CounterexampleKind.LoadFailure,
                "InvalidOperationException: the mutant would not load",
                "removed 1 <AffixTemplate> element(s)",
                CounterfactualVerdict.RequiredToLoad
            ),
            new EvidenceLedger.Row(
                "ordering:edge-cases/feature-system-breadth/phonologicalRules/prAlpha~prHighTrigger",
                CoverageItemKind.Ordering,
                "edge-cases/feature-system-breadth",
                null,
                null,
                CounterexampleKind.None,
                null,
                "swapped adjacent order",
                CounterfactualVerdict.Unobservable
            ),
        };

        EvidenceLedger.Write(_repositoryRoot, rows);
        IReadOnlyList<EvidenceLedger.Row> readBack = EvidenceLedger.Read(_repositoryRoot);

        Assert.That(readBack, Is.EqualTo(rows.OrderBy(r => r.ItemId, StringComparer.Ordinal).ToArray()));
    }

    [Test]
    public void ReadingAnAbsentLedgerReturnsEmpty()
    {
        Assert.That(EvidenceLedger.Read(_repositoryRoot), Is.Empty);
    }

    [Test]
    public void RowsAreWrittenInDeterministicOrder()
    {
        var rows = new[]
        {
            new EvidenceLedger.Row(
                "z-item",
                CoverageItemKind.Surface,
                "fx",
                "w",
                "before",
                CounterexampleKind.Word,
                "after",
                "mutation",
                CounterfactualVerdict.Evidenced
            ),
            new EvidenceLedger.Row(
                "a-item",
                CoverageItemKind.Surface,
                "fx",
                "w",
                "before",
                CounterexampleKind.Word,
                "after",
                "mutation",
                CounterfactualVerdict.Evidenced
            ),
        };

        EvidenceLedger.Write(_repositoryRoot, rows);
        IReadOnlyList<EvidenceLedger.Row> readBack = EvidenceLedger.Read(_repositoryRoot);

        Assert.That(readBack.Select(r => r.ItemId), Is.EqualTo(new[] { "a-item", "z-item" }));
    }

    [Test]
    public void AMalformedLineIsRefused()
    {
        string path = Path.Combine(_repositoryRoot, "conformance", "semantic-coverage-evidence.tsv");
        File.WriteAllText(path, "item_id\tkind\tfixture\texample_word\texample_outcome\tcounterexample_kind\tcounterexample_outcome\tmutation\tverdict\ntoo-few-fields\tSurface\n");

        Assert.Throws<FormatException>(() => EvidenceLedger.Read(_repositoryRoot));
    }

    [TestCase("id")]
    [TestCase("kind")]
    [TestCase("fixture")]
    public void ARowMustMatchTheGeneratedItemBeforeItCanBecomeEvidence(string mismatch)
    {
        var item = new CoverageItem("item", CoverageItemKind.Surface, "dtd-element", "fixture");
        var row = new EvidenceLedger.Row(
            mismatch == "id" ? "other" : "item",
            mismatch == "kind" ? CoverageItemKind.Ordering : CoverageItemKind.Surface,
            mismatch == "fixture" ? "other-fixture" : "fixture",
            "w",
            "before",
            CounterexampleKind.Word,
            "after",
            "mutation",
            CounterfactualVerdict.Evidenced
        );

        Assert.Throws<ArgumentException>(() => EvidenceLedger.ToEvidence(row, item));
    }
}
