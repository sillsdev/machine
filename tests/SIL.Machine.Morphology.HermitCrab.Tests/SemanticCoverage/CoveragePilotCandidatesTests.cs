using System.Xml.Linq;
using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// Pilots the five candidates docs/coverage-pipeline-design.md names, end to end against the real
/// checked-in fixtures: a word counter-example that removes a parse, one that adds a parse, a
/// LoadFailure, a hand-constructed Ordering item, and a Proof-only item.
/// </summary>
[TestFixture]
public sealed class CoveragePilotCandidatesTests
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

    private static SemanticInventory Inventory() => GrammarCoverageGate.ReadInventory(RepositoryRoot());

    private static Fixture FindFixture(string id)
    {
        Fixture? fixture = Fixture
            .DiscoverAll(Path.Combine(RepositoryRoot(), "conformance"))
            .FirstOrDefault(f => f.Id == id);
        Assert.That(fixture, Is.Not.Null, $"fixture '{id}' not found");
        return fixture!;
    }

    private string _scratchDirectory = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _scratchDirectory = Path.Combine(Path.GetTempPath(), "hc-coverage-pilot-tests", Guid.NewGuid().ToString("N"));
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_scratchDirectory))
            Directory.Delete(_scratchDirectory, recursive: true);
    }

    // Candidate 1: word counter-example, parse -> no-parse ('takul': ok::TA+KUL|takul -> ok::-).
    [Test]
    public void Candidate1AffixTemplatesElementRemovesAParse()
    {
        var item = new CoverageItem(
            "dtd:element/AffixTemplates",
            CoverageItemKind.Surface,
            "dtd-element",
            "edge-cases/loader-isactive-breadth"
        );
        Fixture fixture = FindFixture(item.Fixture);
        IReadOnlyList<string> baseline = CounterfactualGate.ComputeBaseline(fixture);
        CounterfactualResult result = CounterfactualGate.Evaluate(fixture, item.Id, Inventory(), baseline, _scratchDirectory);

        Assert.That(result.Verdict, Is.EqualTo(CounterfactualVerdict.Evidenced));
        Assert.That(result.CounterexampleKind, Is.EqualTo(CounterexampleKind.Word));
        Assert.That(result.ExampleOutcome, Does.StartWith("ok::"), "the item intact must produce a real parse");
        Assert.That(result.CounterexampleOutcome, Is.EqualTo("ok::-"), "neutralizing it must lose the parse entirely");

        Evidence evidence = Evidence.FromCounterfactualResult(item.Id, result);
        CompletenessReport gate = CoverageCompletenessGate.Evaluate(
            new[] { item },
            new[] { evidence },
            Array.Empty<Proof>()
        );
        Assert.That(gate.IsComplete, Is.True);
        Assert.That(gate.EvidencedCountsByCounterexampleKind[CounterexampleKind.Word], Is.EqualTo(1));
    }

    // Candidate 2: the OTHER direction, no-parse -> parse ('sol': ok::- -> ok::SOL|sol).
    [Test]
    public void Candidate2AllomorphCoOccurrenceRulesElementAddsAParse()
    {
        var item = new CoverageItem(
            "dtd:element/AllomorphCoOccurrenceRules",
            CoverageItemKind.Surface,
            "dtd-element",
            "edge-cases/morphotactic-attribute-breadth"
        );
        Fixture fixture = FindFixture(item.Fixture);
        IReadOnlyList<string> baseline = CounterfactualGate.ComputeBaseline(fixture);
        CounterfactualResult result = CounterfactualGate.Evaluate(fixture, item.Id, Inventory(), baseline, _scratchDirectory);

        Assert.That(result.Verdict, Is.EqualTo(CounterfactualVerdict.Evidenced));
        Assert.That(result.CounterexampleKind, Is.EqualTo(CounterexampleKind.Word));
        Assert.That(result.ExampleOutcome, Is.EqualTo("ok::-"), "the item intact must have no parse for this word");
        Assert.That(result.CounterexampleOutcome, Does.StartWith("ok::"), "neutralizing it must newly admit a parse");

        Evidence evidence = Evidence.FromCounterfactualResult(item.Id, result);
        CompletenessReport gate = CoverageCompletenessGate.Evaluate(
            new[] { item },
            new[] { evidence },
            Array.Empty<Proof>()
        );
        Assert.That(gate.IsComplete, Is.True);
        Assert.That(gate.EvidencedCountsByCounterexampleKind[CounterexampleKind.Word], Is.EqualTo(1));
    }

    // Candidate 3: counterexampleKind = LoadFailure, the weaker class that must not blend into the
    // Word headline.
    [Test]
    public void Candidate3AffixTemplateElementIsRequiredToLoad()
    {
        var item = new CoverageItem(
            "dtd:element/AffixTemplate",
            CoverageItemKind.Surface,
            "dtd-element",
            "edge-cases/diacritic-segments"
        );
        Fixture fixture = FindFixture(item.Fixture);
        IReadOnlyList<string> baseline = CounterfactualGate.ComputeBaseline(fixture);
        CounterfactualResult result = CounterfactualGate.Evaluate(fixture, item.Id, Inventory(), baseline, _scratchDirectory);

        Assert.That(result.Verdict, Is.EqualTo(CounterfactualVerdict.RequiredToLoad));
        Assert.That(result.CounterexampleKind, Is.EqualTo(CounterexampleKind.LoadFailure));
        Assert.That(result.ExampleWord, Is.Not.Null);
        Assert.That(result.ExampleOutcome, Does.StartWith("ok::"), "the item intact must still load and parse");
        Assert.That(result.CounterexampleOutcome, Does.Contain("Exception"));

        Evidence evidence = Evidence.FromCounterfactualResult(item.Id, result);
        CompletenessReport gate = CoverageCompletenessGate.Evaluate(
            new[] { item },
            new[] { evidence },
            Array.Empty<Proof>()
        );
        Assert.That(gate.IsComplete, Is.True);
        Assert.That(gate.EvidencedCountsByCounterexampleKind[CounterexampleKind.LoadFailure], Is.EqualTo(1));
        Assert.That(
            gate.EvidencedCountsByCounterexampleKind[CounterexampleKind.Word],
            Is.EqualTo(0),
            "a LoadFailure must never be summed into the Word count"
        );
    }

    // Candidate 4: an Ordering item, hand-constructed since the OrderingGenerator does not exist yet.
    // Adjacent swap of prAlpha/prHighTrigger, the two rules declared next to each other in
    // feature-system-breadth's single phonologicalRules list.
    [Test]
    public void Candidate4OrderingItemProvesTheDataModelAgainstAHandBuiltSwap()
    {
        Fixture fixture = FindFixture("edge-cases/feature-system-breadth");
        string[] words = fixture.Words.Words.Select(w => w.Word).ToArray();
        Directory.CreateDirectory(_scratchDirectory);
        IReadOnlyList<string> baseline = CounterfactualGate.EvaluateOneGrammar(fixture.GrammarPath, WriteWordsFile(words));

        XDocument mutant = XDocument.Load(fixture.GrammarPath);
        XAttribute? rules = mutant.Descendants("Stratum").Select(e => e.Attribute("phonologicalRules")).FirstOrDefault(a => a is not null);
        Assert.That(rules, Is.Not.Null);
        string original = rules!.Value;
        Assert.That(original, Does.Contain("prAlpha prHighTrigger"), "pins the adjacency this item swaps");
        rules.Value = original.Replace("prAlpha prHighTrigger", "prHighTrigger prAlpha");

        // No need to place a local HermitCrabInput.dtd copy beside the mutant:
        // XmlLanguageLoader.ResourceXmlResolver resolves that filename to an embedded resource
        // regardless of the document's own directory.
        string mutatedPath = Path.Combine(_scratchDirectory, "mutated.xml");
        mutant.Save(mutatedPath);
        IReadOnlyList<string> mutated = CounterfactualGate.EvaluateOneGrammar(mutatedPath, WriteWordsFile(words));

        var item = new CoverageItem(
            "ordering:edge-cases/feature-system-breadth/phonologicalRules/prAlpha~prHighTrigger",
            CoverageItemKind.Ordering,
            "adjacent-transposition",
            "edge-cases/feature-system-breadth"
        );

        int diffIndex = Enumerable.Range(0, words.Length).FirstOrDefault(i => baseline[i] != mutated[i], -1);

        CompletenessReport gate;
        if (diffIndex >= 0)
        {
            var evidence = new Evidence(
                item.Id,
                item.Fixture,
                words[diffIndex],
                baseline[diffIndex],
                CounterexampleKind.Word,
                mutated[diffIndex],
                "swapped prAlpha and prHighTrigger's declared order in phonologicalRules",
                CounterfactualVerdict.Evidenced
            );
            gate = CoverageCompletenessGate.Evaluate(new[] { item }, new[] { evidence }, Array.Empty<Proof>());
        }
        else
        {
            // No word discriminates the swap, so independence must be recomputed from the grammar, not asserted from the words.
            XDocument unswapped = XDocument.Load(fixture.GrammarPath);
            OrderingItem orderingItem = OrderingGenerator
                .EnumerateAdjacentPairs(unswapped, item.Fixture)
                .Single(candidate => candidate.Id == item.Id);
            Proof? proof = OrderingProofs.TryBuild(unswapped, orderingItem);
            Assert.That(proof, Is.Not.Null, "prAlpha's output and prHighTrigger's input must recompute Disjoint");

            gate = CoverageCompletenessGate.Evaluate(
                new[] { item },
                Array.Empty<Evidence>(),
                new[] { proof! },
                loadGrammar: fixtureId => XDocument.Load(FindFixture(fixtureId).GrammarPath)
            );
        }

        TestContext.Out.WriteLine($"candidate 4 resolved as: {gate.Items.Single().Resolution} ({gate.Items.Single().Detail})");
        Assert.That(gate.IsComplete, Is.True);
    }

    // Candidate 5: the Proof branch -- no evidence exists and none should be manufactured.
    [Test]
    public void Candidate5StratumCyclicityIsProofOnly()
    {
        var item = new CoverageItem(
            "dtd:enum/Stratum/cyclicity/cyclic",
            CoverageItemKind.Surface,
            "dtd-enum",
            "edge-cases/morphotactic-attribute-breadth"
        );

        IReadOnlyList<ImpossibilityProof> proofs = ImpossibilityProofs.Read(RepositoryRoot());
        ImpossibilityProof matched = proofs.Single(p => p.SurfaceId == item.Id);
        var proof = new Proof(item.Id, matched.Kind, matched.Evidence);

        CompletenessReport gate = CoverageCompletenessGate.Evaluate(
            new[] { item },
            Array.Empty<Evidence>(),
            new[] { proof }
        );
        Assert.That(gate.IsComplete, Is.False, "the no-consumer claim remains pending until a mechanical verifier exists");
        Assert.That(gate.Items.Single().Resolution, Is.EqualTo(CoverageResolution.Rejected));

        // The checked-in sweep already recorded this item Unobservable, never Evidenced: the proof is
        // not stale. A proof for something the sweep now evidences must fail instead, per
        // ImpossibilityProofs.Stale.
        IReadOnlyList<CounterfactualResult> checkedInLedger = CounterfactualLedger.Read(RepositoryRoot());
        CounterfactualResult recorded = checkedInLedger.Single(r => r.SurfaceId == item.Id);
        Assert.That(recorded.Verdict, Is.EqualTo(CounterfactualVerdict.Unobservable));
    }

    private string WriteWordsFile(IReadOnlyList<string> words)
    {
        string path = Path.Combine(_scratchDirectory, $"words-{Guid.NewGuid():N}.txt");
        File.WriteAllLines(path, words);
        return path;
    }
}
