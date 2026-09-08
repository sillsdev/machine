using System.Xml.Linq;
using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class DataflowObligationLedgerTests
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

    // Pins the headline counts so a change has to be acknowledged. 40 chains x 4 McDc cells = 160
    // (every chain gates -- see DataflowObligationLedger's own doc comment for why there is no
    // plain/gated split keyed off the required*/excluded* attribute-name prefix); 4 ConditionExtension
    // cells (two MPR-payload chains whose exercising corpus carries a 2-condition
    // requiredMPRFeatures/excludedMPRFeatures value, e.g. mpr-overwrite-order-dependence's
    // "mprP mprQ"); 182 Mutator cells (91 schema-applicable (chain, ObligationMutatorClass) pairs x 2 --
    // see MutatorClassApplicabilityIsSchemaDerived for the per-class breakdown, in particular why
    // CompoundingNonHeadDrop is 12 chains, not a naive 8).
    [Test]
    public void RealCorpusProducesTheDeclaredCellCounts()
    {
        string root = RepositoryRoot();
        IReadOnlyList<DataflowObligationLedger.Row> rows = DataflowObligationLedger.Compute(root);

        int mcDc = rows.Count(r => r.CellKind == "McDc");
        int conditionExtension = rows.Count(r => r.CellKind == "ConditionExtension");
        int mutator = rows.Count(r => r.CellKind == "Mutator");
        int satisfied = rows.Count(r => r.Status == ObligationStatus.Satisfied);
        int notSatisfied = rows.Count(r => r.Status == ObligationStatus.NotSatisfied);
        int unknown = rows.Count(r => r.Status == ObligationStatus.Unknown);

        TestContext.Out.WriteLine(
            $"cells={rows.Count} mcDc={mcDc} conditionExtension={conditionExtension} mutator={mutator} "
                + $"satisfied={satisfied} notSatisfied={notSatisfied} unknown={unknown}"
        );

        Assert.That(rows, Has.Count.EqualTo(346));
        Assert.That(mcDc, Is.EqualTo(160));
        Assert.That(conditionExtension, Is.EqualTo(4));
        Assert.That(mutator, Is.EqualTo(182));
        Assert.That(satisfied + notSatisfied + unknown, Is.EqualTo(rows.Count));
        // Raised 3 -> 4, unknown 165 -> 164: author-coverage-cell added 'ygofz' to
        // languages/fusional-realizational-morphology/words.yaml (MorphologicalPhonologicalRuleFeature::
        // MorphologicalOutput.MPRFeatures->MorphologicalInput.requiredMPRFeatures::McDc:AbsentGatedForm).
        // GOF's own lexically-preset mprPedA is destroyed by mrThemeY's Overwrite-group write before
        // mrEndZ's requiredMPRFeatures gate is checked; severing either mrThemeY's own
        // MorphologicalOutput.MPRFeatures write (so the overwrite never triggers) or mrEndZ's own
        // requiredMPRFeatures (so the gate stops needing mprPedA) unblocks the SAME word identically.
        Assert.That(satisfied, Is.EqualTo(9));
        Assert.That(notSatisfied, Is.EqualTo(120));
        Assert.That(unknown, Is.EqualTo(217));
    }

    // Mutator-class applicability is schema/engine-derived (payload type + writer + -- for
    // CompoundingNonHeadDrop -- reader), never corpus-derived -- pins the exact (class -> chain count)
    // mapping ApplicableMutatorClasses computes: Blocking on every chain (40), Overwrite scoped to the
    // MPR-feature junction (24 chains), PosPriorityUnion scoped to the PartOfSpeech junction (15
    // chains), CompoundingNonHeadDrop scoped to all three MPR writers but ONLY the
    // MorphologicalInput/PhonologicalSubrule readers (12 chains) -- engine-verified NOT to reach
    // CompoundingRule.headProdRestrictionsMprFeatures/HeadMorphologicalInput's own gates (which read the
    // pre-drop head state) or CompoundingRule.nonHeadProdRestrictionsMprFeatures (analysis-direction
    // only, reads the lexicon entry directly), so a naive "writer is LexicalEntry.ruleFeatures" guess of
    // 8 is both too narrow (misses the other two MPR writers reaching the same two readers) and too
    // wide (includes four provably-inert readers).
    [Test]
    public void MutatorClassApplicabilityIsSchemaDerived()
    {
        string root = RepositoryRoot();
        IReadOnlyList<DataflowObligationLedger.Row> rows = DataflowObligationLedger.Compute(root);
        DataflowObligationLedger.Row[] mutatorRows = rows.Where(r => r.CellKind == "Mutator").ToArray();

        (string ClassName, int ExpectedCells)[] expected =
        {
            ("Blocking", 80),
            ("Overwrite", 48),
            ("PosPriorityUnion", 30),
            ("CompoundingNonHeadDrop", 24),
        };
        foreach ((string className, int expectedCells) in expected)
        {
            int actual = mutatorRows.Count(r => r.MutatorClass == className);
            Assert.That(actual, Is.EqualTo(expectedCells), $"{className} cell count");
        }
    }

    // CompoundingNonHeadDrop's reader restriction, pinned directly: the writer/reader pairs it must
    // (not) generate obligations for, independent of the cell-count arithmetic above.
    [Test]
    public void CompoundingNonHeadDropExcludesTheProvenInertReaders()
    {
        string root = RepositoryRoot();
        DataflowObligationLedger.Row[] cndRows = DataflowObligationLedger
            .Compute(root)
            .Where(r => r.MutatorClass == "CompoundingNonHeadDrop")
            .ToArray();

        var pairs = cndRows.Select(r => (r.ReaderElement, r.ReaderAttribute)).Distinct().ToArray();

        Assert.That(
            pairs,
            Is.EquivalentTo(
                new[]
                {
                    ("MorphologicalInput", "requiredMPRFeatures"),
                    ("MorphologicalInput", "excludedMPRFeatures"),
                    ("PhonologicalSubrule", "requiredMPRFeatures"),
                    ("PhonologicalSubrule", "excludedMPRFeatures"),
                }
            )
        );
        Assert.That(
            cndRows.Select(r => r.WriterElement).Distinct(),
            Is.EquivalentTo(new[] { "LexicalEntry", "MorphologicalOutput", "CompoundingRule" })
        );
    }

    // The nine chains this generator has mechanically confirmed via a same-word PAIR witness (severing
    // writer AND reader both flip the SAME word from a failed to a successful parse -- see
    // DataflowObligationLedger.FindPairedWitness). Two were found by the mechanical scan (vokadan in
    // mpr-gated-exception, nunavuq in polysynthetic-stratal-derivation-chain); the rest were authored.
    // The recurring shape worth knowing: a feature the ROOT presets cannot witness a required-gate
    // chain, because severing it can only turn a passing word into a failing one. What works is a
    // feature an AFFIX confers or destroys before the reader checks it -- ygofz established that against
    // MorphologicalInput, and the PhonologicalSubrule pairs reuse it against a sound-rule reader.
    // Every other exercised chain stays Unknown -- deliberately: "writer witnessed somewhere, reader
    // witnessed somewhere" is not this bar.
    [Test]
    public void SatisfiedCellsAreExactlyTheMechanicallyConfirmedPairWitnesses()
    {
        string root = RepositoryRoot();
        IReadOnlyList<DataflowObligationLedger.Row> rows = DataflowObligationLedger.Compute(root);
        DataflowObligationLedger.Row[] satisfied = rows.Where(r => r.Status == ObligationStatus.Satisfied).ToArray();

        Assert.That(satisfied, Has.Length.EqualTo(9));
        Assert.That(
            satisfied.Select(r => (r.WriterElement, r.WriterAttribute, r.ReaderElement, r.ReaderAttribute, r.Role)),
            Is.EquivalentTo(
                new[]
                {
                    ("LexicalEntry", "ruleFeatures", "MorphologicalInput", "excludedMPRFeatures", "PresentGatedForm"),
                    ("LexicalEntry", "ruleFeatures", "PhonologicalSubrule", "excludedMPRFeatures", "PresentGatedForm"),
                    ("LexicalEntry", "partOfSpeech", "MorphologicalRule", "requiredPartsOfSpeech", "AbsentGatedForm"),
                    ("LexicalEntry", "partOfSpeech", "PhonologicalSubrule", "requiredPartsOfSpeech", "AbsentGatedForm"),
                    (
                        "MorphologicalOutput",
                        "MPRFeatures",
                        "MorphologicalInput",
                        "excludedMPRFeatures",
                        "PresentGatedForm"
                    ),
                    (
                        "MorphologicalOutput",
                        "MPRFeatures",
                        "MorphologicalInput",
                        "requiredMPRFeatures",
                        "AbsentGatedForm"
                    ),
                    (
                        "MorphologicalOutput",
                        "MPRFeatures",
                        "PhonologicalSubrule",
                        "excludedMPRFeatures",
                        "PresentGatedForm"
                    ),
                    (
                        "MorphologicalOutput",
                        "MPRFeatures",
                        "PhonologicalSubrule",
                        "requiredMPRFeatures",
                        "AbsentGatedForm"
                    ),
                    (
                        "CompoundingRule",
                        "outputPartOfSpeech",
                        "MorphologicalRule",
                        "requiredPartsOfSpeech",
                        "AbsentGatedForm"
                    ),
                }
            )
        );
        Assert.That(satisfied.All(r => r.Evidence.Contains("paired witness")), Is.True);
    }

    // An unexercised chain (InteractionChainLedger.Row.Exercised false) cannot witness any of its cells
    // -- every McDc and Mutator cell for such a chain must be NotSatisfied, unambiguously, never Unknown.
    [Test]
    public void UnexercisedChainsProduceOnlyNotSatisfiedCells()
    {
        string root = RepositoryRoot();
        IReadOnlyList<InteractionChainLedger.Row> chains = InteractionChainLedger.Compute(root);
        IReadOnlyList<DataflowObligationLedger.Row> rows = DataflowObligationLedger.Compute(root);

        InteractionChainLedger.Row unexercised = chains.First(c => !c.Exercised);
        DataflowObligationLedger.Row[] cellsForChain = rows.Where(r =>
                r.WriterElement == unexercised.WriterElement
                && r.WriterAttribute == unexercised.WriterAttribute
                && r.ReaderElement == unexercised.ReaderElement
                && r.ReaderAttribute == unexercised.ReaderAttribute
            )
            .ToArray();

        Assert.That(cellsForChain, Is.Not.Empty);
        Assert.That(cellsForChain.All(r => r.Status == ObligationStatus.NotSatisfied), Is.True);
    }

    // Cell ids are unique and deterministic from (payload type, writer, reader, cell kind, role,
    // mutator class) alone -- none of which move when unrelated corpus content changes, only status
    // does. Regenerating twice over an unchanged DTD/engine/corpus must agree byte for byte, and no two
    // distinct cells may collide.
    [Test]
    public void CellIdsAreUniqueAndReproducible()
    {
        string root = RepositoryRoot();
        IReadOnlyList<DataflowObligationLedger.Row> first = DataflowObligationLedger.Compute(root);
        IReadOnlyList<DataflowObligationLedger.Row> second = DataflowObligationLedger.Compute(root);

        Assert.That(first.Select(r => r.CellId).Distinct().Count(), Is.EqualTo(first.Count));
        Assert.That(DataflowObligationLedger.ToText(first), Is.EqualTo(DataflowObligationLedger.ToText(second)));
    }

    // Mirrors InteractionChainLedgerTests.CheckedInInteractionChainLedgerIsUpToDate: regenerate the
    // ledger and require the checked-in file to match byte for byte.
    [Test]
    public void CheckedInDataflowObligationLedgerIsUpToDate()
    {
        string root = RepositoryRoot();
        IReadOnlyList<DataflowObligationLedger.Row> rows = DataflowObligationLedger.Compute(root);

        string fresh = DataflowObligationLedger.ToText(rows);
        string checkedIn = File.ReadAllText(
            Path.Combine(root, DataflowObligationLedger.RelativePath.Replace('/', Path.DirectorySeparatorChar))
        );

        Assert.That(
            fresh.ReplaceLineEndings("\n"),
            Is.EqualTo(checkedIn.ReplaceLineEndings("\n")),
            "regenerate with: hc-conformance --write-dataflow-obligations --repository-root ."
        );
    }

    // ------------------------------------------------------------------------------------------
    // MutatorClassDetectors: unit-level pins for the three structural detectors directly, independent
    // of which real fixtures currently exercise them.
    // ------------------------------------------------------------------------------------------

    private const string NoFamilyGrammar = """
        <HermitCrabInput><Language><Strata>
          <Stratum><Name>S</Name>
            <LexicalEntries>
              <LexicalEntry><Allomorphs><RootAllomorph id="a1"><Shape>a</Shape></RootAllomorph></Allomorphs></LexicalEntry>
            </LexicalEntries>
          </Stratum>
        </Strata></Language></HermitCrabInput>
        """;

    private const string FamilySameStratumGrammar = """
        <HermitCrabInput><Language>
          <Families><Family id="fam1">fam</Family></Families>
          <Strata>
            <Stratum><Name>S</Name>
              <LexicalEntries>
                <LexicalEntry family="fam1"><Allomorphs><RootAllomorph id="a1"><Shape>a</Shape></RootAllomorph></Allomorphs></LexicalEntry>
                <LexicalEntry family="fam1"><Allomorphs><RootAllomorph id="a2"><Shape>b</Shape></RootAllomorph></Allomorphs></LexicalEntry>
              </LexicalEntries>
            </Stratum>
          </Strata>
        </Language></HermitCrabInput>
        """;

    private const string FamilyDifferentStrataGrammar = """
        <HermitCrabInput><Language>
          <Families><Family id="fam1">fam</Family></Families>
          <Strata>
            <Stratum><Name>S1</Name>
              <LexicalEntries>
                <LexicalEntry family="fam1"><Allomorphs><RootAllomorph id="a1"><Shape>a</Shape></RootAllomorph></Allomorphs></LexicalEntry>
              </LexicalEntries>
            </Stratum>
            <Stratum><Name>S2</Name>
              <LexicalEntries>
                <LexicalEntry family="fam1"><Allomorphs><RootAllomorph id="a2"><Shape>b</Shape></RootAllomorph></Allomorphs></LexicalEntry>
              </LexicalEntries>
            </Stratum>
          </Strata>
        </Language></HermitCrabInput>
        """;

    [Test]
    public void HasEligibleFamilyRequiresTwoSiblingsUnderTheSameStratum()
    {
        Assert.That(MutatorClassDetectors.HasEligibleFamily(XDocument.Parse(NoFamilyGrammar)), Is.False);
        Assert.That(MutatorClassDetectors.HasEligibleFamily(XDocument.Parse(FamilySameStratumGrammar)), Is.True);
        Assert.That(MutatorClassDetectors.HasEligibleFamily(XDocument.Parse(FamilyDifferentStrataGrammar)), Is.False);
    }

    private const string NoPosWritingRuleGrammar = """
        <HermitCrabInput><Language><Strata>
          <Stratum><Name>S</Name>
            <MorphologicalRuleDefinitions>
              <MorphologicalRule id="mA"><Name>rA</Name></MorphologicalRule>
            </MorphologicalRuleDefinitions>
          </Stratum>
        </Strata></Language></HermitCrabInput>
        """;

    private const string TwoPosWritingRulesGrammar = """
        <HermitCrabInput><Language><Strata>
          <Stratum><Name>S</Name>
            <MorphologicalRuleDefinitions>
              <MorphologicalRule id="mA" outputPartOfSpeech="posX"><Name>rA</Name></MorphologicalRule>
              <MorphologicalRule id="mB" outputPartOfSpeech="posY"><Name>rB</Name></MorphologicalRule>
            </MorphologicalRuleDefinitions>
          </Stratum>
        </Strata></Language></HermitCrabInput>
        """;

    [Test]
    public void CountPosWritingRuleElementsCountsOnlyNonEmptyOutputPartOfSpeech()
    {
        Assert.That(
            MutatorClassDetectors.CountPosWritingRuleElements(XDocument.Parse(NoPosWritingRuleGrammar)),
            Is.EqualTo(0)
        );
        Assert.That(
            MutatorClassDetectors.CountPosWritingRuleElements(XDocument.Parse(TwoPosWritingRulesGrammar)),
            Is.EqualTo(2)
        );
    }

    private const string NoCompoundingRuleGrammar = """
        <HermitCrabInput><Language><Strata>
          <Stratum><Name>S</Name></Stratum>
        </Strata></Language></HermitCrabInput>
        """;

    private const string HasCompoundingRuleGrammar = """
        <HermitCrabInput><Language><Strata>
          <Stratum><Name>S</Name>
            <MorphologicalRuleDefinitions>
              <CompoundingRule id="cA"><Name>rC</Name></CompoundingRule>
            </MorphologicalRuleDefinitions>
          </Stratum>
        </Strata></Language></HermitCrabInput>
        """;

    [Test]
    public void HasCompoundingRuleDetectsPresence()
    {
        Assert.That(MutatorClassDetectors.HasCompoundingRule(XDocument.Parse(NoCompoundingRuleGrammar)), Is.False);
        Assert.That(MutatorClassDetectors.HasCompoundingRule(XDocument.Parse(HasCompoundingRuleGrammar)), Is.True);
    }
}
