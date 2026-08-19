using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class EvidenceCardGeneratorTests
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

    // The two cell ids docs/dataflow-coverage-plan.md's own worked examples name, and
    // DataflowClaimGateTests already seeds/pins as real, currently-Satisfied, currently-claimed cells.
    // Deliberately does NOT pin the ledger's total cell count or per-status breakdown -- both move as
    // the concurrent obligation-fill work lands (see this task's own instruction not to pin counts
    // that work will move).
    private const string VokadanCellId =
        "MorphologicalPhonologicalRuleFeature::LexicalEntry.ruleFeatures->MorphologicalInput.excludedMPRFeatures::McDc:PresentGatedForm";
    private const string NunavuqCellId =
        "PartOfSpeech::LexicalEntry.partOfSpeech->MorphologicalRule.requiredPartsOfSpeech::McDc:AbsentGatedForm";

    // One card per ledger row, and every card gets a distinct file name (Compute itself throws on a
    // collision, so reaching this assertion at all is already load-bearing).
    [Test]
    public void OneCardPerLedgerRowWithDistinctFileNames()
    {
        string root = RepositoryRoot();
        IReadOnlyList<DataflowObligationLedger.Row> ledgerRows = DataflowObligationLedger.Read(root);
        IReadOnlyList<EvidenceCard> cards = EvidenceCardGenerator.Compute(root);

        Assert.That(cards, Has.Count.EqualTo(ledgerRows.Count));
        Assert.That(cards.Select(c => c.CellId).ToHashSet(), Is.EquivalentTo(ledgerRows.Select(r => r.CellId).ToHashSet()));
        Assert.That(cards.Select(c => c.FileName).Distinct().Count(), Is.EqualTo(cards.Count));
    }

    // The vokadan cell is the worked example throughout docs/dataflow-coverage-plan.md and
    // docs/coverage-review-protocol.md: this asserts its card actually carries everything the
    // reviewer's four checks (predict, cite lines, check counterpart, read prose against values) need
    // in one screen, not spread across four files.
    [Test]
    public void VokadanCardCarriesEveryReviewerIngredient()
    {
        string root = RepositoryRoot();
        EvidenceCard card = EvidenceCardGenerator.Compute(root).Single(c => c.CellId == VokadanCellId);

        Assert.That(card.Markdown, Does.Contain("the payload IS present and the gated form IS blocked."));
        Assert.That(card.Markdown, Does.Contain("**Satisfied**"));
        Assert.That(card.Markdown, Does.Contain("Claimed by word **'vokadan'** in `edge-cases/mpr-gated-exception`"));
        // Grammar citations: the writer's payload and the reader's gate, each at a real line number
        // (not merely "present somewhere").
        Assert.That(card.Markdown, Does.Match(@"Writer \(payload declared here\) `LexicalEntry\.ruleFeatures`: `grammar\.xml:\d+` = ""mprException"""));
        Assert.That(card.Markdown, Does.Match(@"Reader \(gate declared here\) `MorphologicalInput\.excludedMPRFeatures`: `grammar\.xml:\d+` = ""mprException"""));
        // Before/after, both from the author's own claim and independently from the machine witness
        // ledger -- two separate facts, never collapsed into one.
        Assert.That(card.Markdown, Does.Contain("Before: `ok::-`"));
        Assert.That(card.Markdown, Does.Contain("After: `ok::VOKAD+SUF|vokadan`"));
        Assert.That(card.Markdown, Does.Contain("verdict=Evidenced"));
        Assert.That(card.Markdown, Does.Contain("'vokadan': ok::- -> ok::VOKAD+SUF|vokadan"));
        // The author's proof prose, verbatim.
        Assert.That(card.Markdown, Does.Contain("the PresentGatedForm arm: the feature is PRESENT on the root"));
        // distinct_from, with BOTH outcomes shown so a reviewer can see they differ without opening
        // words.yaml.
        Assert.That(card.Markdown, Does.Contain("`distinct_from` **'sanitan'** (expect_fail=False) vs. this word (expect_fail=True)"));
    }

    [Test]
    public void NunavuqCardIdentifiesItsOwnFixtureAndWord()
    {
        string root = RepositoryRoot();
        EvidenceCard card = EvidenceCardGenerator.Compute(root).Single(c => c.CellId == NunavuqCellId);

        Assert.That(card.Markdown, Does.Contain("'nunavuq'"));
        Assert.That(card.Markdown, Does.Contain("languages/polysynthetic-stratal-derivation-chain"));
        Assert.That(card.Markdown, Does.Contain("the payload is ABSENT and the gated form IS blocked."));
    }

    // Falsification (constraint #2 of the task): a cell with none of claim/prose/counterpart must say
    // so explicitly in every section, never leave a section blank -- checked across every card in the
    // real corpus, not just one cherry-picked example.
    [Test]
    public void EveryCardRendersEverySectionNeverBlank()
    {
        string root = RepositoryRoot();
        IReadOnlyList<EvidenceCard> cards = EvidenceCardGenerator.Compute(root);
        string[] requiredHeaders =
        {
            "## Role, in plain English",
            "## Chain",
            "## Machine status",
            "## Fixture and word",
            "## Exact mutation and before/after parse",
            "## Grammar citations",
            "## Author's prose",
            "## `distinct_from` counterpart",
        };

        foreach (EvidenceCard card in cards)
        {
            string[] lines = card.Markdown.ReplaceLineEndings("\n").Split('\n');
            foreach (string header in requiredHeaders)
            {
                int headerIndex = Array.IndexOf(lines, header);
                Assert.That(headerIndex, Is.GreaterThanOrEqualTo(0), $"{card.CellId}: missing header '{header}'");

                // The next non-blank line after a header is the section's content; a section is never
                // immediately followed by another "## " header (that would mean nothing was rendered).
                int i = headerIndex + 1;
                while (i < lines.Length && lines[i].Length == 0)
                    i++;
                Assert.That(
                    i < lines.Length && !lines[i].StartsWith("## ", StringComparison.Ordinal),
                    $"{card.CellId}: section '{header}' has no content before the next header"
                );
            }
        }
    }

    // At least one real cell has no claimed_cells entry naming it and no extractable witnessing word
    // (every NotSatisfied cell qualifies, since an unexercised chain's evidence names no fixture at
    // all) -- confirms that specific, common case renders the explicit "no claim / no word" prose
    // rather than something that merely happens to look non-blank.
    [Test]
    public void UnclaimedCellWithNoIdentifiableWordRendersAbsenceExplicitly()
    {
        string root = RepositoryRoot();
        IReadOnlyList<DataflowObligationLedger.Row> ledgerRows = DataflowObligationLedger.Read(root);
        DataflowObligationLedger.Row unexercised = ledgerRows.First(r => r.Status == ObligationStatus.NotSatisfied);
        EvidenceCard card = EvidenceCardGenerator.Compute(root).Single(c => c.CellId == unexercised.CellId);

        Assert.That(card.Markdown, Does.Contain("No fixture or word is identified for this cell"));
        Assert.That(card.Markdown, Does.Contain("No `claimed_cells` entry recorded an author-reviewed severing/before/after"));
        Assert.That(card.Markdown, Does.Contain("No prose recorded: no claim, and no word is identified for this cell."));
        Assert.That(card.Markdown, Does.Contain("No claim exists for this cell, so no `distinct_from` counterpart is recorded."));
    }

    // Falsification (task's own requirement): the drift gate must catch a hand-edit. Isolated from the
    // real corpus and from Compute() entirely -- this exercises Write()/Check() directly against a
    // throwaway directory with two synthetic cards, so it neither depends on nor perturbs the checked-in
    // conformance/evidence-cards/ directory.
    [Test]
    public void DriftCheckCatchesAHandEditAMissingFileAndAnExtraFile()
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), $"hc-evidence-card-drift-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        try
        {
            var cards = new[]
            {
                new EvidenceCard("cell-one", "cell-one__aaaa.md", "# cell one\ncontent\n"),
                new EvidenceCard("cell-two", "cell-two__bbbb.md", "# cell two\ncontent\n"),
            };

            EvidenceCardGenerator.Write(tempRoot, cards);
            Assert.That(EvidenceCardGenerator.Check(tempRoot, cards).IsCurrent, Is.True, "a freshly written set must check as current");

            string cardDir = Path.Combine(tempRoot, "conformance", "evidence-cards");

            // Hand-edit.
            string editedPath = Path.Combine(cardDir, "cell-one__aaaa.md");
            File.WriteAllText(editedPath, "# cell one\nHAND-EDITED, NOT REGENERATED\n");
            EvidenceCardDiff editedDiff = EvidenceCardGenerator.Check(tempRoot, cards);
            Assert.That(editedDiff.IsCurrent, Is.False);
            Assert.That(editedDiff.Details, Has.Some.Contains("STALE cell-one"));

            // Restore, then delete a file outright.
            File.WriteAllText(editedPath, "# cell one\ncontent\n");
            Assert.That(EvidenceCardGenerator.Check(tempRoot, cards).IsCurrent, Is.True, "restoring the hand-edit must check as current again");

            File.Delete(editedPath);
            EvidenceCardDiff missingDiff = EvidenceCardGenerator.Check(tempRoot, cards);
            Assert.That(missingDiff.IsCurrent, Is.False);
            Assert.That(missingDiff.Details, Has.Some.Contains("MISSING cell-one"));

            // Restore, then add a stray extra file.
            EvidenceCardGenerator.Write(tempRoot, cards);
            File.WriteAllText(Path.Combine(cardDir, "stray-leftover.md"), "not a generated card\n");
            EvidenceCardDiff extraDiff = EvidenceCardGenerator.Check(tempRoot, cards);
            Assert.That(extraDiff.IsCurrent, Is.False);
            Assert.That(extraDiff.Details, Has.Some.Contains("EXTRA FILE stray-leftover.md"));

            // Write() itself removes anything not in the fresh set.
            EvidenceCardGenerator.Write(tempRoot, cards);
            Assert.That(File.Exists(Path.Combine(cardDir, "stray-leftover.md")), Is.False);
            Assert.That(EvidenceCardGenerator.Check(tempRoot, cards).IsCurrent, Is.True);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    // Sanity check that what is actually checked in right now is the fresh render -- i.e. this task's
    // own generated conformance/evidence-cards/ directory is not stale relative to its own generator.
    [Test]
    public void CheckedInCardsAreCurrent()
    {
        string root = RepositoryRoot();
        IReadOnlyList<EvidenceCard> cards = EvidenceCardGenerator.Compute(root);
        EvidenceCardDiff diff = EvidenceCardGenerator.Check(root, cards);

        Assert.That(diff.IsCurrent, Is.True, string.Join("\n", diff.Details));
    }
}
