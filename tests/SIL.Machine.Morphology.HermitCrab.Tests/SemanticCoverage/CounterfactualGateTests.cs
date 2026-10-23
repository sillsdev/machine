using System.Xml.Linq;
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
    public void ANonAsciiSurfaceFormSurvivesTheMutantChildProcess()
    {
        // The mutant's outcomes cross a pipe; the baseline does not. If the pipe re-encodes "ɯ", the
        // unchanged parse misses its baseline and Gloss counts as Evidenced.
        File.WriteAllText(
            Path.Combine(_fixtureDirectory, "grammar.xml"),
            ProbeGrammar
                .Replace("<Representation>a</Representation>", "<Representation>ɯ</Representation>")
                .Replace("<PhoneticShape>a</PhoneticShape>", "<PhoneticShape>ɯ</PhoneticShape>")
        );
        var words = new WordsYaml { Language = "CounterfactualProbe" };
        words.Words.Add(new WordEntry { Word = "ɯ" });
        var fixture = new Fixture("probe/fixture", _fixtureDirectory, words);
        IReadOnlyList<string> baseline = CounterfactualGate.ComputeBaseline(fixture);

        CounterfactualResult result = CounterfactualGate.Evaluate(
            fixture,
            "dtd:element/Gloss",
            Inventory(),
            baseline,
            _scratchDirectory
        );

        Assert.That(baseline.Single(), Does.Contain("ɯ"));
        Assert.That(result.Verdict, Is.EqualTo(CounterfactualVerdict.Unobservable), result.Delta);
    }

    [Test]
    [NonParallelizable]
    public void AMutantThatRunsOutOfMemoryIsATimeoutNotEvidence()
    {
        // xxxxxxk has 924 analyses, enough to exhaust a 64 MB heap; the probe grammar fits in less than
        // the runtime needs to start at all.
        string grammar = File.ReadAllText(
            Path.Combine(RepositoryRoot(), "conformance", "edge-cases", "deep-optional-affix-nesting", "grammar.xml")
        );
        File.WriteAllText(
            Path.Combine(_fixtureDirectory, "grammar.xml"),
            grammar.Replace("<MorphemeId>K</MorphemeId>", "<MorphemeId>K</MorphemeId><Gloss>k</Gloss>")
        );
        var words = new WordsYaml { Language = "DeepOptionalAffixNesting" };
        words.Words.Add(new WordEntry { Word = "xxxxxxk" });
        var fixture = new Fixture("probe/fixture", _fixtureDirectory, words);
        IReadOnlyList<string> baseline = CounterfactualGate.ComputeBaseline(fixture);

        // The child inherits this; the test host, already running, does not.
        string? previous = Environment.GetEnvironmentVariable("DOTNET_GCHeapHardLimit");
        Environment.SetEnvironmentVariable("DOTNET_GCHeapHardLimit", "0x4000000");
        CounterfactualResult result;
        try
        {
            result = CounterfactualGate.Evaluate(
                fixture,
                "dtd:element/Gloss",
                Inventory(),
                baseline,
                _scratchDirectory
            );
        }
        finally
        {
            Environment.SetEnvironmentVariable("DOTNET_GCHeapHardLimit", previous);
        }

        Assert.That(result.Verdict, Is.EqualTo(CounterfactualVerdict.Timeout), result.Delta);
        Assert.That(result.Delta, Does.Contain("GC heap limit"));
    }

    [Test]
    public void DeletingTheRootLanguageElementIsRequiredByDtd()
    {
        Fixture fixture = ProbeFixture();
        IReadOnlyList<string> baseline = CounterfactualGate.ComputeBaseline(fixture);

        // Language is an element surface: removing it fails generic DTD content-model validation
        // (HermitCrabInput's declared sequence) before XmlLanguageLoader ever runs.
        CounterfactualResult result = CounterfactualGate.Evaluate(
            fixture,
            "dtd:element/Language",
            Inventory(),
            baseline,
            _scratchDirectory
        );

        Assert.That(result.Verdict, Is.EqualTo(CounterfactualVerdict.RequiredByDtd));
    }

    [Test]
    public void AMutantChildThatRefusesTheGrammarReportsARejection()
    {
        XDocument mutant = XDocument.Parse(ProbeGrammar);
        mutant.Root!.Element("Language")!.Remove();
        Directory.CreateDirectory(_scratchDirectory);
        string mutatedPath = Path.Combine(_scratchDirectory, "rejected.xml");
        mutant.Save(mutatedPath);

        Assert.Throws<GrammarRejectedException>(() => CounterfactualGate.EvaluateWithTimeout(mutatedPath, ["a"]));
    }

    [Test]
    public void AMutantChildThatOverflowsTheStackReportsAResourceLimit()
    {
        // Without its required MPR features, one of this fixture's words recurses until the stack overflows;
        // the loader accepts the grammar.
        Fixture fixture = Fixture
            .DiscoverAll(Path.Combine(RepositoryRoot(), "conformance"))
            .Single(f => f.Id == "languages/suffixing-extension-slot-ordering");
        XDocument mutant = XDocument.Load(fixture.GrammarPath);
        foreach (XElement input in mutant.Descendants("MorphologicalInput"))
            input.Attribute("requiredMPRFeatures")?.Remove();
        Directory.CreateDirectory(_scratchDirectory);
        string mutatedPath = Path.Combine(_scratchDirectory, "overflowing.xml");
        mutant.Save(mutatedPath);

        string[] words = fixture.Words.Words.Select(word => word.Word).ToArray();

        Assert.Throws<StackLimitExceededException>(() => CounterfactualGate.EvaluateWithTimeout(mutatedPath, words));
    }

    [Test]
    public void AMutantChildThatFailsForAnotherReasonIsNotARejection()
    {
        Directory.CreateDirectory(_scratchDirectory);
        string missingPath = Path.Combine(_scratchDirectory, "missing.xml");

        Assert.Throws<InvalidOperationException>(() => CounterfactualGate.EvaluateWithTimeout(missingPath, ["a"]));
    }

    [Test]
    public void AMutantChildsWordListIsNotWrittenBesideTheGrammar()
    {
        // The finally that deletes the word list does not run if the sweep is killed, so a file
        // written beside a fixture's grammar.xml would be left in the checkout.
        Directory.CreateDirectory(_scratchDirectory);
        string grammarPath = Path.Combine(_scratchDirectory, "grammar.xml");
        var created = new System.Collections.Concurrent.ConcurrentBag<string>();
        using var watcher = new FileSystemWatcher(_scratchDirectory) { EnableRaisingEvents = true };
        watcher.Created += (_, e) => created.Add(e.Name ?? "");

        Assert.Throws<InvalidOperationException>(() => CounterfactualGate.EvaluateWithTimeout(grammarPath, ["a"]));
        // Created events arrive asynchronously; give one written during the call time to land.
        Thread.Sleep(500);

        Assert.That(created, Is.Empty);
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
            Does.Contain("RequiredByLoader"),
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
        Assert.That(
            baseline,
            Is.EqualTo(new[] { "ok::KIMB+RED|kimbiakimbia" }),
            "pins the baseline this test reasons from"
        );

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

    [Test]
    public void FirstSiblingRequiredByLoaderThenLaterSiblingEvidencedResultsInEvidencedWithWordCounterexample()
    {
        var requiredToLoad = new CounterfactualResult(
            "s",
            "fx",
            CounterfactualVerdict.RequiredByLoader,
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
            CounterfactualVerdict.RequiredByLoader,
            "m1",
            "d1",
            CounterexampleKind: CounterexampleKind.LoadFailure
        );
        var second = new CounterfactualResult(
            "s",
            "fx",
            CounterfactualVerdict.RequiredByLoader,
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

    [Test]
    public void ConsiderEnumSiblingResultKeepsATimeoutUntilAStrongerResultFollows()
    {
        var timeout = new CounterfactualResult("s", "fx", CounterfactualVerdict.Timeout, "m1", "d1");
        var unobservable = new CounterfactualResult("s", "fx", CounterfactualVerdict.Unobservable, "m2", "d2");
        var requiredToLoad = new CounterfactualResult(
            "s",
            "fx",
            CounterfactualVerdict.RequiredByLoader,
            "m3",
            "d3",
            CounterexampleKind: CounterexampleKind.LoadFailure
        );

        (CounterfactualResult? best1, _) = CounterfactualGate.ConsiderEnumSiblingResult(null, timeout);
        (CounterfactualResult? best2, _) = CounterfactualGate.ConsiderEnumSiblingResult(best1, unobservable);
        Assert.That(best2, Is.EqualTo(timeout), "an all-timeout search must not be reported as Unobservable");

        (CounterfactualResult? best3, _) = CounterfactualGate.ConsiderEnumSiblingResult(best2, requiredToLoad);
        Assert.That(best3, Is.EqualTo(requiredToLoad), "a load failure outranks a timeout");
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
