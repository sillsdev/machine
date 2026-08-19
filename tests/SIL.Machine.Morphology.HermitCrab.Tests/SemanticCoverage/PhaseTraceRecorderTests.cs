using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// Pins the observation channel a phase-level claim rests on: that the recorder really does
/// separate unapplication from application from the final verdict on a real grammar, and that the
/// digest is stable enough to diff two runs against each other.
/// </summary>
[TestFixture]
public sealed class PhaseTraceRecorderTests
{
    private const string Fixture = "languages/templatic-root-modification";
    private const string Word = "katab";

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

    private static PhaseTraceRecorder Parse(string word)
    {
        string grammar = Path.Combine(
            RepositoryRoot(),
            "conformance",
            Fixture.Replace('/', Path.DirectorySeparatorChar),
            "grammar.xml");
        Language language = XmlLanguageLoader.Load(grammar);
        var recorder = new PhaseTraceRecorder();
        var morpher = new Morpher(recorder, language);
        morpher.ParseWord(word, out _).ToArray();
        return recorder;
    }

    [Test]
    public void EachPhaseRecordsItsOwnEvents()
    {
        PhaseTraceRecorder recorder = Parse(Word);

        Assert.Multiple(() =>
        {
            Assert.That(
                recorder.Events(ParsePhase.AnalysisCandidate),
                Is.Not.Empty,
                "unapplication must be observable");
            Assert.That(
                recorder.Events(ParsePhase.SynthesisConfirmation),
                Is.Not.Empty,
                "application must be observable");
            Assert.That(
                recorder.Events(ParsePhase.FinalParse),
                Is.Not.Empty,
                "a parsed word must reach a verdict");
        });
    }

    // A phase claim is only falsifiable if each phase's events are exclusive to it; if an
    // application event could surface in the analysis bucket, a synthesis-only effect would read as
    // an analysis effect too.
    [Test]
    public void PhasesDoNotShareEventKinds()
    {
        PhaseTraceRecorder recorder = Parse(Word);

        string[] analysis = Kinds(recorder, ParsePhase.AnalysisCandidate);
        string[] synthesis = Kinds(recorder, ParsePhase.SynthesisConfirmation);
        string[] final = Kinds(recorder, ParsePhase.FinalParse);

        Assert.Multiple(() =>
        {
            Assert.That(analysis.Intersect(synthesis, StringComparer.Ordinal), Is.Empty);
            Assert.That(analysis.Intersect(final, StringComparer.Ordinal), Is.Empty);
            Assert.That(synthesis.Intersect(final, StringComparer.Ordinal), Is.Empty);
            Assert.That(analysis, Does.Contain("BeginUnapplyStratum"));
            Assert.That(synthesis, Does.Contain("BeginApplyStratum"));
        });
    }

    // Phase membership was wrong for the hinge event and no test noticed: asserting only that the
    // phases share no event kind passes whichever phase a kind is filed under. Lexical lookup is
    // called by Morpher.Synthesize, so the engine places it at the start of the build-up, not at
    // the end of the tear-down.
    [Test]
    public void TheLexicalLookupHingeIsRecordedOnTheBuildUpSide()
    {
        PhaseTraceRecorder recorder = Parse(Word);

        Assert.Multiple(() =>
        {
            Assert.That(Kinds(recorder, ParsePhase.SynthesisConfirmation), Does.Contain("LexicalLookup"));
            Assert.That(Kinds(recorder, ParsePhase.AnalysisCandidate), Does.Not.Contain("LexicalLookup"));
        });
    }

    // The counterfactual compares digests across two grammars, so a digest that moved between two
    // runs of the SAME grammar would report differences that are not semantic ones.
    [Test]
    public void TheDigestIsStableAcrossRunsOfTheSameGrammar()
    {
        PhaseTraceRecorder first = Parse(Word);
        PhaseTraceRecorder second = Parse(Word);

        Assert.Multiple(() =>
        {
            foreach (ParsePhase phase in Enum.GetValues<ParsePhase>())
                Assert.That(second.Digest(phase), Is.EqualTo(first.Digest(phase)), phase.ToString());
        });
    }

    // A different word must move the trace, or the digest is measuring nothing.
    [Test]
    public void ADifferentWordProducesADifferentAnalysisDigest()
    {
        Assert.That(
            Parse("katabit").Digest(ParsePhase.AnalysisCandidate),
            Is.Not.EqualTo(Parse(Word).Digest(ParsePhase.AnalysisCandidate)));
    }

    private static string[] Kinds(PhaseTraceRecorder recorder, ParsePhase phase) =>
        recorder
            .Events(phase)
            .Select(item => item.Contains('(', StringComparison.Ordinal) ? item[..item.IndexOf('(', StringComparison.Ordinal)] : item)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
}
