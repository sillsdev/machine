using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// Checks the source scan and the fixture re-run behind the `not-in-signature` proof kind. No checked-in
/// proof uses the kind now: the only one, for `dtd:element/Properties`, was retired with the two fixtures
/// that declared the element, so the fixture re-run is tested on synthetic fixtures in a scratch root.
/// </summary>
[TestFixture]
public sealed class NotInSignatureCheckTests
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

    // A minimal DTD-valid grammar with one root "a". The gloss line is replaced per fixture.
    private const string ProbeGrammar = """
        <?xml version="1.0" encoding="utf-8"?>
        <!DOCTYPE HermitCrabInput SYSTEM "HermitCrabInput.dtd">
        <HermitCrabInput>
          <Language>
            <Name>NotInSignatureProbe</Name>
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
                    GLOSS
                  </LexicalEntry>
                </LexicalEntries>
              </Stratum>
            </Strata>
          </Language>
        </HermitCrabInput>
        """;

    private const string ProbeWords = """
        language: NotInSignatureProbe
        inspired_by: ["synthetic probe"]
        sources: ["synthetic probe"]
        requires: []
        fieldworks_producible: true
        words:
          - word: a
            note: the root alone
            parses:
              - signature: "ROOT|a"
                rules: []
        """;

    private string _scratchRoot = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _scratchRoot = Path.Combine(Path.GetTempPath(), "hc-not-in-signature-tests", Guid.NewGuid().ToString("N"));
        string conformance = Path.Combine(_scratchRoot, "conformance");
        Directory.CreateDirectory(conformance);
        File.Copy(
            Path.Combine(RepositoryRoot(), "conformance", "HermitCrabInput.dtd"),
            Path.Combine(conformance, "HermitCrabInput.dtd")
        );
        AddFixture("glossed", "<Gloss>root-gloss</Gloss>");
        AddFixture("bare", string.Empty);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_scratchRoot))
            Directory.Delete(_scratchRoot, recursive: true);
    }

    private void AddFixture(string name, string glossLine)
    {
        string directory = Path.Combine(_scratchRoot, "conformance", "edge-cases", name);
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "grammar.xml"), ProbeGrammar.Replace("GLOSS", glossLine));
        File.WriteAllText(Path.Combine(directory, "words.yaml"), ProbeWords);
    }

    [Test]
    public void FixturesContainingListsOnlyTheFixturesThatDeclareTheElement()
    {
        Assert.That(
            NotInSignatureCheck.FixturesContaining(_scratchRoot, "Gloss"),
            Is.EqualTo(new[] { "edge-cases/glossed" })
        );
        Assert.That(NotInSignatureCheck.FixturesContaining(_scratchRoot, "Properties"), Is.Empty);
    }

    [Test]
    public void NeutralizingAnElementNothingReadsChangesNoWordInTheFixturesThatDeclareIt()
    {
        bool verified = NotInSignatureCheck.MutationChangesNoWordInAnyContainingFixture(
            _scratchRoot,
            "dtd:element/Gloss",
            "Gloss",
            Path.Combine(_scratchRoot, "scratch")
        );

        Assert.That(verified, Is.True);
    }

    [Test]
    public void NeutralizingAnElementTheSignatureUsesIsNotVerifiedAsNotInSignature()
    {
        bool verified = NotInSignatureCheck.MutationChangesNoWordInAnyContainingFixture(
            _scratchRoot,
            "dtd:element/MorphemeId",
            "MorphemeId",
            Path.Combine(_scratchRoot, "scratch")
        );

        Assert.That(verified, Is.False, "removing MorphemeId changes the parse signature");
    }

    [Test]
    public void AnElementNoFixtureDeclaresIsNotVerified()
    {
        bool verified = NotInSignatureCheck.MutationChangesNoWordInAnyContainingFixture(
            _scratchRoot,
            "dtd:element/Properties",
            "Properties",
            Path.Combine(_scratchRoot, "scratch")
        );

        Assert.That(verified, Is.False, "an untested claim is not evidence");
    }

    [Test]
    public void SignatureFormatSourceNeverReadsProperties()
    {
        Assert.That(NotInSignatureCheck.SignatureSourceNeverReads(RepositoryRoot(), "Properties"), Is.True);
    }

    // Sanity check for the negative direction of the source scan: a property BuildSignature genuinely
    // does read (Morpheme.Id, which every signature entry is built from) must not pass.
    [Test]
    public void SignatureSourceScanDoesDetectAPropertyThatIsActuallyRead()
    {
        Assert.That(NotInSignatureCheck.SignatureSourceNeverReads(RepositoryRoot(), "Id"), Is.False);
    }
}
