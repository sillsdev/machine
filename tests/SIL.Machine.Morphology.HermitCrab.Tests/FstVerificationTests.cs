using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// The shadow/verification gate (HERMITCRAB_FST_PLAN.md §9.5): FstVerification.Compare measures
/// FST-vs-search analysis-set parity over a corpus — confirming completeness (no missing) and
/// soundness (no spurious) at once, the certificate required before the FST may replace search.
/// </summary>
public class FstVerificationTests : HermitCrabTestBase
{
    private AffixProcessRule AddSuffix()
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        var sSuffix = new AffixProcessRule
        {
            Name = "s_suffix",
            Gloss = "NMLZ",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
            OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("N").Value,
        };
        sSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, "s") },
            }
        );
        Morphophonemic.MorphologicalRules.Add(sSuffix);
        return sSuffix;
    }

    private sealed class EmptyAnalyzer : IMorphologicalAnalyzer
    {
        public IEnumerable<WordAnalysis> AnalyzeWord(string word) => Enumerable.Empty<WordAnalysis>();
    }

    [Test]
    public void Compare_FstVsSearch_IsCompleteOnConcatenativeCorpus()
    {
        AffixProcessRule suffix = AddSuffix();
        IMorphologicalAnalyzer search = new Morpher(TraceManager, Language);
        IMorphologicalAnalyzer fst = new VerifiedFstAnalyzer(TraceManager, Language);

        // A mix: inflected, bare root, homograph (dat = entries 8 & 9), and a non-word.
        string[] corpus = { "sag", "sags", "dat", "sagg" };
        AnalysisComparison comparison = FstVerification.Compare(search, fst, corpus);

        Assert.That(comparison.WordsChecked, Is.EqualTo(corpus.Length));
        Assert.That(comparison.IsComplete, Is.True, comparison.Format());

        Morphophonemic.MorphologicalRules.Remove(suffix);
    }

    [Test]
    public void Compare_DetectsMissingAnalyses_NotVacuous()
    {
        AffixProcessRule suffix = AddSuffix();
        IMorphologicalAnalyzer search = new Morpher(TraceManager, Language);

        // A candidate that finds nothing must be flagged incomplete on a word that has an analysis.
        AnalysisComparison comparison = FstVerification.Compare(search, new EmptyAnalyzer(), new[] { "sag" });

        Assert.That(comparison.IsComplete, Is.False);
        Assert.That(comparison.Divergences, Has.Count.EqualTo(1));
        Assert.That(comparison.Divergences[0].MissingFromCandidate, Is.Not.Empty);
        Assert.That(comparison.Divergences[0].ExtraInCandidate, Is.Empty);

        Morphophonemic.MorphologicalRules.Remove(suffix);
    }
}
