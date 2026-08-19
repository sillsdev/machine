using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class CounterfactualGateTests
{
    // A minimal, DTD-valid grammar with exactly one root ("a") so each test can neutralize one
    // surface and know precisely what should (or should not) change.
    private const string ProbeGrammar = """
        <?xml version="1.0" encoding="utf-8"?>
        <!DOCTYPE HermitCrabInput SYSTEM "HermitCrabInput.dtd">
        <HermitCrabInput>
          <Language>
            <Name>CounterfactualProbe</Name>
            <PartsOfSpeech><PartOfSpeech id="posAny"><Name>Any</Name></PartOfSpeech></PartsOfSpeech>
            <CharacterDefinitionTable id="t1">
              <Name>Table</Name>
              <SegmentDefinitions>
                <SegmentDefinition id="ca"><Representations><Representation>a</Representation></Representations></SegmentDefinition>
              </SegmentDefinitions>
            </CharacterDefinitionTable>
            <Strata>
              <Stratum characterDefinitionTable="t1">
                <Name>Only</Name>
                <LexicalEntries>
                  <LexicalEntry id="eRoot">
                    <Allomorphs><Allomorph id="aRoot"><PhoneticShape>a</PhoneticShape></Allomorph></Allomorphs>
                    <MorphemeId>ROOT</MorphemeId>
                    <Gloss>root-gloss</Gloss>
                  </LexicalEntry>
                </LexicalEntries>
              </Stratum>
            </Strata>
          </Language>
        </HermitCrabInput>
        """;

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

    private string _fixtureDirectory = string.Empty;
    private string _scratchDirectory = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _fixtureDirectory = Path.Combine(
            Path.GetTempPath(),
            "hc-counterfactual-gate-tests",
            Guid.NewGuid().ToString("N")
        );
        _scratchDirectory = Path.Combine(_fixtureDirectory, "scratch");
        Directory.CreateDirectory(_fixtureDirectory);
        File.WriteAllText(Path.Combine(_fixtureDirectory, "grammar.xml"), ProbeGrammar);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_fixtureDirectory))
            Directory.Delete(_fixtureDirectory, recursive: true);
    }

    private Fixture ProbeFixture()
    {
        var words = new WordsYaml { Language = "CounterfactualProbe" };
        words.Words.Add(new WordEntry { Word = "a" });
        return new Fixture("probe/fixture", _fixtureDirectory, words);
    }

    [Test]
    public void ASurfaceWhoseMutationChangesAResultIsEvidenced()
    {
        Fixture fixture = ProbeFixture();
        IReadOnlyList<string> baseline = CounterfactualGate.ComputeBaseline(fixture);

        // MorphemeId is optional per the DTD, so deleting it leaves a loadable document -- but the
        // loader falls back to a null morpheme ID, which changes the parse signature.
        CounterfactualResult result = CounterfactualGate.Evaluate(
            fixture,
            "dtd:element/MorphemeId",
            Inventory(),
            baseline,
            _scratchDirectory
        );

        Assert.That(result.Verdict, Is.EqualTo(CounterfactualVerdict.Evidenced));
        Assert.That(result.Delta, Does.Contain("->"));
    }

    [Test]
    public void ASurfaceWhoseMutationChangesNothingIsUnobservable()
    {
        Fixture fixture = ProbeFixture();
        IReadOnlyList<string> baseline = CounterfactualGate.ComputeBaseline(fixture);

        // Gloss is never read by the parser or folded into a signature, so removing it cannot
        // change any word's outcome.
        CounterfactualResult result = CounterfactualGate.Evaluate(
            fixture,
            "dtd:element/Gloss",
            Inventory(),
            baseline,
            _scratchDirectory
        );

        Assert.That(result.Verdict, Is.EqualTo(CounterfactualVerdict.Unobservable));
    }

    [Test]
    public void DeletingTheRootLanguageElementIsRequiredToLoad()
    {
        Fixture fixture = ProbeFixture();
        IReadOnlyList<string> baseline = CounterfactualGate.ComputeBaseline(fixture);

        CounterfactualResult result = CounterfactualGate.Evaluate(
            fixture,
            "dtd:element/Language",
            Inventory(),
            baseline,
            _scratchDirectory
        );

        Assert.That(result.Verdict, Is.EqualTo(CounterfactualVerdict.RequiredToLoad));
    }

    [Test]
    public void TheGateDoesNotMutateTheOriginalDocumentOnDisk()
    {
        Fixture fixture = ProbeFixture();
        IReadOnlyList<string> baseline = CounterfactualGate.ComputeBaseline(fixture);
        string before = File.ReadAllText(fixture.GrammarPath);

        CounterfactualGate.Evaluate(fixture, "dtd:element/MorphemeId", Inventory(), baseline, _scratchDirectory);
        CounterfactualGate.Evaluate(fixture, "dtd:element/Language", Inventory(), baseline, _scratchDirectory);

        Assert.That(File.ReadAllText(fixture.GrammarPath), Is.EqualTo(before));
    }

    // A fail-fast-IDREF probe mirroring the real loader-isactive-breadth shape: an inactive Family
    // ("famX") that only an equally inactive LexicalEntry ("eB") names. Activating famX alone
    // changes nothing (nothing references it); activating eB alone throws at load (the family it
    // names was never loaded); only activating both lets "b" parse.
    private const string JointFamilyProbeGrammar = """
        <?xml version="1.0" encoding="utf-8"?>
        <!DOCTYPE HermitCrabInput SYSTEM "HermitCrabInput.dtd">
        <HermitCrabInput>
          <Language>
            <Name>JointFamilyProbe</Name>
            <PartsOfSpeech><PartOfSpeech id="posAny"><Name>Any</Name></PartOfSpeech></PartsOfSpeech>
            <CharacterDefinitionTable id="t1">
              <Name>Table</Name>
              <SegmentDefinitions>
                <SegmentDefinition id="ca"><Representations><Representation>a</Representation></Representations></SegmentDefinition>
                <SegmentDefinition id="cb"><Representations><Representation>b</Representation></Representations></SegmentDefinition>
              </SegmentDefinitions>
            </CharacterDefinitionTable>
            <Families>
              <Family id="famX" isActive="no">X</Family>
            </Families>
            <Strata>
              <Stratum characterDefinitionTable="t1">
                <Name>Only</Name>
                <LexicalEntries>
                  <LexicalEntry id="eRoot">
                    <Allomorphs><Allomorph id="aRoot"><PhoneticShape>a</PhoneticShape></Allomorph></Allomorphs>
                    <MorphemeId>ROOT</MorphemeId>
                    <Gloss>root-gloss</Gloss>
                  </LexicalEntry>
                  <LexicalEntry id="eB" isActive="no" family="famX">
                    <Allomorphs><Allomorph id="aB"><PhoneticShape>b</PhoneticShape></Allomorph></Allomorphs>
                    <MorphemeId>B</MorphemeId>
                    <Gloss>b-gloss</Gloss>
                  </LexicalEntry>
                </LexicalEntries>
              </Stratum>
            </Strata>
          </Language>
        </HermitCrabInput>
        """;

    // A tolerant-IDREF probe where the "partner" (an inactive Slot naming both mrX and the
    // already-active mrY) changes "ya" all by itself -- mrY needs no help from mrX to reach the
    // slot -- so the joint delta must NOT be credited to MorphologicalRule/isActive/no for mrX.
    private const string PartnerAloneExplainsItGrammar = """
        <?xml version="1.0" encoding="utf-8"?>
        <!DOCTYPE HermitCrabInput SYSTEM "HermitCrabInput.dtd">
        <HermitCrabInput>
          <Language>
            <Name>JointRejectProbe</Name>
            <PartsOfSpeech><PartOfSpeech id="posAny"><Name>Any</Name></PartOfSpeech></PartsOfSpeech>
            <CharacterDefinitionTable id="t1">
              <Name>Table</Name>
              <SegmentDefinitions>
                <SegmentDefinition id="ca"><Representations><Representation>a</Representation></Representations></SegmentDefinition>
                <SegmentDefinition id="cx"><Representations><Representation>x</Representation></Representations></SegmentDefinition>
                <SegmentDefinition id="cy"><Representations><Representation>y</Representation></Representations></SegmentDefinition>
              </SegmentDefinitions>
            </CharacterDefinitionTable>
            <NaturalClasses>
              <FeatureNaturalClass id="ncAny"><Name>Any</Name></FeatureNaturalClass>
            </NaturalClasses>
            <Strata>
              <Stratum characterDefinitionTable="t1" morphologicalRuleOrder="unordered">
                <Name>Only</Name>
                <MorphologicalRuleDefinitions>
                  <MorphologicalRule id="mrX" isActive="no">
                    <Name>xPrefix</Name>
                    <MorphologicalSubrules>
                      <MorphologicalSubrule id="subX">
                        <MorphologicalInput><PhoneticSequence id="stemX"><OptionalSegmentSequence min="1" max="-1"><SimpleContext naturalClass="ncAny" /></OptionalSegmentSequence></PhoneticSequence></MorphologicalInput>
                        <MorphologicalOutput><InsertSegments><PhoneticShape>x</PhoneticShape></InsertSegments><CopyFromInput index="stemX" /></MorphologicalOutput>
                      </MorphologicalSubrule>
                    </MorphologicalSubrules>
                    <MorphemeId>X</MorphemeId>
                    <Gloss>x-gloss</Gloss>
                  </MorphologicalRule>
                  <MorphologicalRule id="mrY">
                    <Name>yPrefix</Name>
                    <MorphologicalSubrules>
                      <MorphologicalSubrule id="subY">
                        <MorphologicalInput><PhoneticSequence id="stemY"><OptionalSegmentSequence min="1" max="-1"><SimpleContext naturalClass="ncAny" /></OptionalSegmentSequence></PhoneticSequence></MorphologicalInput>
                        <MorphologicalOutput><InsertSegments><PhoneticShape>y</PhoneticShape></InsertSegments><CopyFromInput index="stemY" /></MorphologicalOutput>
                      </MorphologicalSubrule>
                    </MorphologicalSubrules>
                    <MorphemeId>Y</MorphemeId>
                    <Gloss>y-gloss</Gloss>
                  </MorphologicalRule>
                </MorphologicalRuleDefinitions>
                <AffixTemplates>
                  <AffixTemplate>
                    <Name>tmpl</Name>
                    <Slot isActive="no" optional="true" morphologicalRules="mrX mrY"><Name>slot</Name></Slot>
                  </AffixTemplate>
                </AffixTemplates>
                <LexicalEntries>
                  <LexicalEntry id="eRoot">
                    <Allomorphs><Allomorph id="aRoot"><PhoneticShape>a</PhoneticShape></Allomorph></Allomorphs>
                    <MorphemeId>ROOT</MorphemeId>
                    <Gloss>root-gloss</Gloss>
                  </LexicalEntry>
                </LexicalEntries>
              </Stratum>
            </Strata>
          </Language>
        </HermitCrabInput>
        """;

    private Fixture ProbeFixtureFor(string grammar, string language, params string[] words)
    {
        File.WriteAllText(Path.Combine(_fixtureDirectory, "grammar.xml"), grammar);
        var wordsYaml = new WordsYaml { Language = language };
        foreach (string word in words)
            wordsYaml.Words.Add(new WordEntry { Word = word });
        return new Fixture("probe/joint-fixture", _fixtureDirectory, wordsYaml);
    }

    [Test]
    public void ASurfaceEvidencedOnlyJointlyIsReportedAsEvidencedJointly()
    {
        Fixture fixture = ProbeFixtureFor(JointFamilyProbeGrammar, "JointFamilyProbe", "a", "b");
        IReadOnlyList<string> baseline = CounterfactualGate.ComputeBaseline(fixture);

        CounterfactualResult single = CounterfactualGate.Evaluate(
            fixture,
            "dtd:enum/Family/isActive/no",
            Inventory(),
            baseline,
            _scratchDirectory
        );
        Assert.That(
            single.Verdict,
            Is.EqualTo(CounterfactualVerdict.Unobservable),
            "flipping famX alone must change nothing"
        );

        CounterfactualResult joint = CounterfactualGate.EvaluateJointly(
            fixture,
            "dtd:enum/Family/isActive/no",
            Inventory(),
            baseline,
            _scratchDirectory
        );

        Assert.That(joint.Verdict, Is.EqualTo(CounterfactualVerdict.EvidencedJointly));
        Assert.That(joint.Delta, Does.Contain("target alone"));
        Assert.That(joint.Delta, Does.Contain("partner alone"));
        Assert.That(
            joint.Delta,
            Does.Contain("RequiredToLoad"),
            "the partner alone must throw, not silently drop the reference"
        );
        Assert.That(joint.Delta, Does.Contain("'b'"), "the joint delta must name the word that newly parses");
    }

    [Test]
    public void ADeltaThePartnerAloneAlreadyExplainsIsNotEvidenced()
    {
        Fixture fixture = ProbeFixtureFor(PartnerAloneExplainsItGrammar, "JointRejectProbe", "a", "ya");
        IReadOnlyList<string> baseline = CounterfactualGate.ComputeBaseline(fixture);

        CounterfactualResult joint = CounterfactualGate.EvaluateJointly(
            fixture,
            "dtd:enum/MorphologicalRule/isActive/no",
            Inventory(),
            baseline,
            _scratchDirectory
        );

        Assert.That(
            joint.Verdict,
            Is.EqualTo(CounterfactualVerdict.Unobservable),
            "the slot alone already makes 'ya' parse via mrY; mrX contributes nothing extra on this word list"
        );
        Assert.That(joint.Delta, Does.Contain("partner alone reproduces the identical delta"));
    }

    [Test]
    public void NoIndependentPartnerYieldsUnobservableNotAThrow()
    {
        // eB itself is filtered out (isActive="no"), so mrX/famX above are both eligible targets in
        // GrammarFeatureUsage terms, but a surface this grammar never declares at all has no target
        // to even look for a partner around.
        Fixture fixture = ProbeFixtureFor(JointFamilyProbeGrammar, "JointFamilyProbe", "a", "b");
        IReadOnlyList<string> baseline = CounterfactualGate.ComputeBaseline(fixture);

        CounterfactualResult result = CounterfactualGate.EvaluateJointly(
            fixture,
            "dtd:enum/FeatureNaturalClass/isActive/no",
            Inventory(),
            baseline,
            _scratchDirectory
        );

        Assert.That(result.Verdict, Is.EqualTo(CounterfactualVerdict.Unobservable));
        Assert.That(result.Delta, Does.Contain("no independently-inactive referencing declaration"));
    }

    // mrNever requires a part of speech no lexical entry carries, so it never applies and none of
    // multipleApplication's nine declared sibling values can change any outcome.
    private const string RuleNeverAppliesGrammar = """
        <?xml version="1.0" encoding="utf-8"?>
        <!DOCTYPE HermitCrabInput SYSTEM "HermitCrabInput.dtd">
        <HermitCrabInput>
          <Language>
            <Name>MultipleApplicationProbe</Name>
            <PartsOfSpeech>
              <PartOfSpeech id="posOther"><Name>Other</Name></PartOfSpeech>
            </PartsOfSpeech>
            <CharacterDefinitionTable id="t1">
              <Name>Table</Name>
              <SegmentDefinitions>
                <SegmentDefinition id="ca"><Representations><Representation>a</Representation></Representations></SegmentDefinition>
                <SegmentDefinition id="cx"><Representations><Representation>x</Representation></Representations></SegmentDefinition>
              </SegmentDefinitions>
            </CharacterDefinitionTable>
            <NaturalClasses>
              <FeatureNaturalClass id="ncAny"><Name>Any</Name></FeatureNaturalClass>
            </NaturalClasses>
            <Strata>
              <Stratum characterDefinitionTable="t1">
                <Name>Only</Name>
                <MorphologicalRuleDefinitions>
                  <MorphologicalRule id="mrNever" requiredPartsOfSpeech="posOther" multipleApplication="1">
                    <Name>never</Name>
                    <MorphologicalSubrules>
                      <MorphologicalSubrule id="subNever">
                        <MorphologicalInput><PhoneticSequence id="stemNever"><OptionalSegmentSequence min="1" max="-1"><SimpleContext naturalClass="ncAny" /></OptionalSegmentSequence></PhoneticSequence></MorphologicalInput>
                        <MorphologicalOutput><InsertSegments><PhoneticShape>x</PhoneticShape></InsertSegments><CopyFromInput index="stemNever" /></MorphologicalOutput>
                      </MorphologicalSubrule>
                    </MorphologicalSubrules>
                    <MorphemeId>NEVER</MorphemeId>
                  </MorphologicalRule>
                </MorphologicalRuleDefinitions>
                <LexicalEntries>
                  <LexicalEntry id="eRoot">
                    <Allomorphs><Allomorph id="aRoot"><PhoneticShape>a</PhoneticShape></Allomorph></Allomorphs>
                    <MorphemeId>ROOT</MorphemeId>
                    <Gloss>root-gloss</Gloss>
                  </LexicalEntry>
                </LexicalEntries>
              </Stratum>
            </Strata>
          </Language>
        </HermitCrabInput>
        """;

    [Test]
    public void ASurfaceUnobservableAgainstEverySiblingRecordsTheFullList()
    {
        Fixture fixture = ProbeFixtureFor(RuleNeverAppliesGrammar, "MultipleApplicationProbe", "a");
        IReadOnlyList<string> baseline = CounterfactualGate.ComputeBaseline(fixture);

        CounterfactualResult result = CounterfactualGate.Evaluate(
            fixture,
            "dtd:enum/MorphologicalRule/multipleApplication/1",
            Inventory(),
            baseline,
            _scratchDirectory
        );

        Assert.That(result.Verdict, Is.EqualTo(CounterfactualVerdict.Unobservable));
        Assert.That(result.Mutation, Does.Contain("9 declared sibling"));
        foreach (string sibling in new[] { "0", "2", "3", "4", "5", "6", "7", "8", "9" })
            Assert.That(result.Mutation, Does.Contain($"\"{sibling}\""), $"sibling {sibling} must be named");
        Assert.That(result.Delta, Does.Contain("none produced a delta"));
    }

    [Test]
    public void RepeatedEvaluationsOfAnUnobservableEnumSurfaceAgreeOnTheSiblingsTried()
    {
        Fixture fixture = ProbeFixtureFor(RuleNeverAppliesGrammar, "MultipleApplicationProbe", "a");
        IReadOnlyList<string> baseline = CounterfactualGate.ComputeBaseline(fixture);

        CounterfactualResult first = CounterfactualGate.Evaluate(
            fixture,
            "dtd:enum/MorphologicalRule/multipleApplication/1",
            Inventory(),
            baseline,
            _scratchDirectory
        );
        CounterfactualResult second = CounterfactualGate.Evaluate(
            fixture,
            "dtd:enum/MorphologicalRule/multipleApplication/1",
            Inventory(),
            baseline,
            _scratchDirectory
        );

        Assert.That(second.Verdict, Is.EqualTo(first.Verdict));
        Assert.That(second.Mutation, Is.EqualTo(first.Mutation));
        Assert.That(second.Delta, Is.EqualTo(first.Delta));
    }

    // redupMorphType's alphabetically-first sibling "implicit" is a no-op on this fixture, but the
    // untried "prefix" sibling swaps the morph order -- the proven false-negative this fixes.
    [Test]
    public void ASurfaceWhoseFirstSiblingIsANoOpButALaterSiblingDiscriminatesIsEvidenced()
    {
        string root = RepositoryRoot();
        string fixtureDirectory = Path.Combine(root, "conformance", "languages", "suffixing-extension-slot-ordering");
        var words = new WordsYaml { Language = "SuffixingExtensionSlotOrdering" };
        words.Words.Add(new WordEntry { Word = "kimbiakimbia" });
        var fixture = new Fixture("languages/suffixing-extension-slot-ordering", fixtureDirectory, words);

        IReadOnlyList<string> baseline = CounterfactualGate.ComputeBaseline(fixture);
        Assert.That(baseline, Is.EqualTo(new[] { "ok::KIMB+RED|kimbiakimbia" }), "pins the baseline this test reasons from");

        CounterfactualResult result = CounterfactualGate.Evaluate(
            fixture,
            "dtd:enum/MorphologicalOutput/redupMorphType/suffix",
            Inventory(),
            baseline,
            _scratchDirectory
        );

        Assert.That(result.Verdict, Is.EqualTo(CounterfactualVerdict.Evidenced));
        Assert.That(result.Mutation, Does.Contain("to \"prefix\""), "the discriminating sibling must be named");
        Assert.That(result.Delta, Does.Contain("sibling \"prefix\""));
        Assert.That(result.Delta, Does.Contain("KIMB+RED"));
        Assert.That(result.Delta, Does.Contain("RED+KIMB"));
    }

    // The enum-sibling search must not stop at the first RequiredToLoad it finds: Word and
    // LoadFailure are different strengths of evidence (CounterexampleKind), and a later sibling's
    // word-level contrast is stronger. Stopping early would make the recorded strength depend on the
    // DTD's alphabetical value ordering rather than on what the grammar shows.
    [Test]
    public void FirstSiblingRequiredToLoadThenLaterSiblingEvidencedResultsInEvidencedWithWordCounterexample()
    {
        var requiredToLoad = new CounterfactualResult(
            "s",
            "fx",
            CounterfactualVerdict.RequiredToLoad,
            "rewrote to sibling A",
            "InvalidOperationException: boom",
            CounterexampleKind: CounterexampleKind.LoadFailure,
            CounterexampleOutcome: "InvalidOperationException: boom"
        );
        var evidenced = new CounterfactualResult(
            "s",
            "fx",
            CounterfactualVerdict.Evidenced,
            "rewrote to sibling B",
            "'w': ok::X -> ok::Y",
            ExampleWord: "w",
            ExampleOutcome: "ok::X",
            CounterexampleKind: CounterexampleKind.Word,
            CounterexampleOutcome: "ok::Y"
        );

        (CounterfactualResult? bestAfterFirst, CounterfactualGate.EnumSiblingSearchAction actionAfterFirst) =
            CounterfactualGate.ConsiderEnumSiblingResult(null, requiredToLoad);
        Assert.That(bestAfterFirst, Is.EqualTo(requiredToLoad));
        Assert.That(
            actionAfterFirst,
            Is.EqualTo(CounterfactualGate.EnumSiblingSearchAction.Continue),
            "a load failure alone must not stop the search for a stronger result"
        );

        (CounterfactualResult? bestAfterSecond, CounterfactualGate.EnumSiblingSearchAction actionAfterSecond) =
            CounterfactualGate.ConsiderEnumSiblingResult(bestAfterFirst, evidenced);
        Assert.That(bestAfterSecond, Is.EqualTo(evidenced));
        Assert.That(bestAfterSecond!.Verdict, Is.EqualTo(CounterfactualVerdict.Evidenced));
        Assert.That(bestAfterSecond.CounterexampleKind, Is.EqualTo(CounterexampleKind.Word));
        Assert.That(actionAfterSecond, Is.EqualTo(CounterfactualGate.EnumSiblingSearchAction.Stop));
    }

    [Test]
    public void ConsiderEnumSiblingResultStopsImmediatelyOnWordEvidence()
    {
        var evidenced = new CounterfactualResult(
            "s",
            "fx",
            CounterfactualVerdict.Evidenced,
            "m",
            "d",
            CounterexampleKind: CounterexampleKind.Word
        );

        (CounterfactualResult? best, CounterfactualGate.EnumSiblingSearchAction action) =
            CounterfactualGate.ConsiderEnumSiblingResult(null, evidenced);

        Assert.That(best, Is.EqualTo(evidenced));
        Assert.That(action, Is.EqualTo(CounterfactualGate.EnumSiblingSearchAction.Stop));
    }

    [Test]
    public void ConsiderEnumSiblingResultKeepsTheFirstLoadFailureWhenNoStrongerResultFollows()
    {
        var first = new CounterfactualResult(
            "s",
            "fx",
            CounterfactualVerdict.RequiredToLoad,
            "m1",
            "d1",
            CounterexampleKind: CounterexampleKind.LoadFailure
        );
        var second = new CounterfactualResult(
            "s",
            "fx",
            CounterfactualVerdict.RequiredToLoad,
            "m2",
            "d2",
            CounterexampleKind: CounterexampleKind.LoadFailure
        );
        var unobservable = new CounterfactualResult("s", "fx", CounterfactualVerdict.Unobservable, "m3", "d3");

        (CounterfactualResult? best1, _) = CounterfactualGate.ConsiderEnumSiblingResult(null, first);
        (CounterfactualResult? best2, _) = CounterfactualGate.ConsiderEnumSiblingResult(best1, second);
        (CounterfactualResult? best3, _) = CounterfactualGate.ConsiderEnumSiblingResult(best2, unobservable);

        Assert.That(best3, Is.EqualTo(first), "the first load failure wins when nothing stronger is ever found");
    }

    // Recomputes the whole sweep, so it costs minutes and is excluded from the default suite; the
    // counterfactual-coverage workflow is what runs it.
    [Test]
    [Explicit("re-parses every fixture once per surface")]
    [Category("Counterfactual")]
    public void TheCheckedInLedgerMatchesAFreshRecomputeExactly()
    {
        string root = RepositoryRoot();
        IReadOnlyList<CounterfactualResult> fresh = CounterfactualLedger.Sweep(root, Inventory());
        IReadOnlyList<CounterfactualResult> checkedIn = CounterfactualLedger.Read(root);

        var freshById = fresh.ToDictionary(entry => entry.SurfaceId, StringComparer.Ordinal);
        var checkedInById = checkedIn.ToDictionary(entry => entry.SurfaceId, StringComparer.Ordinal);

        string[] added = freshById
            .Keys.Except(checkedInById.Keys, StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        string[] removed = checkedInById
            .Keys.Except(freshById.Keys, StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        string[] changed = freshById
            .Keys.Intersect(checkedInById.Keys, StringComparer.Ordinal)
            .Where(id => freshById[id] != checkedInById[id])
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(
                added,
                Is.Empty,
                $"new surfaces; regenerate with --write-counterfactual:\n  {string.Join("\n  ", added)}"
            );
            Assert.That(
                removed,
                Is.Empty,
                $"stale surfaces; delete from {CounterfactualLedger.RelativePath}:\n  {string.Join("\n  ", removed)}"
            );
            Assert.That(
                changed,
                Is.Empty,
                $"changed verdicts; regenerate with --write-counterfactual after checking why:\n  {string.Join("\n  ", changed)}"
            );
        });
    }
}
