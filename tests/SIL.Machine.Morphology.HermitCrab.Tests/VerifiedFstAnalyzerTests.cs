using System.Collections.Concurrent;
using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// CI coverage for the propose-and-verify spine (HERMITCRAB_FST_PLAN.md §11.8/§12): the FST proposes,
/// HC's own engine confirms each candidate by restricted re-analysis (<see cref="FstReplay"/>), and
/// the confirmed engine analysis is emitted. Exercises soundness (no false positives), the M2 fix
/// (yields genuine HC analyses with their category), the per-word opt-out, and thread-safety.
/// </summary>
public class VerifiedFstAnalyzerTests : HermitCrabTestBase
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

    [Test]
    public void Verified_MatchesSearch_OnConcatenativeCorpus()
    {
        AffixProcessRule suffix = AddSuffix();
        IMorphologicalAnalyzer search = new Morpher(TraceManager, Language);
        IMorphologicalAnalyzer verified = new VerifiedFstAnalyzer(TraceManager, Language);
        string[] corpus = { "sag", "sags", "dat", "sagg" }; // inflected, bare, homograph, non-word
        AnalysisComparison comparison = FstVerification.Compare(search, verified, corpus);
        Assert.That(comparison.IsComplete, Is.True, comparison.Format());
        Morphophonemic.MorphologicalRules.Remove(suffix);
    }

    [Test]
    public void Verified_RejectsNonWord_NoFalsePositive()
    {
        IMorphologicalAnalyzer search = new Morpher(TraceManager, Language);
        IMorphologicalAnalyzer verified = new VerifiedFstAnalyzer(TraceManager, Language);
        Assert.That(search.AnalyzeWord("sagg"), Is.Empty, "precondition: sagg is a non-word");
        Assert.That(verified.AnalyzeWord("sagg"), Is.Empty, "verify must not analyze a non-word");
    }

    [Test]
    public void Verified_YieldsGenuineEngineAnalyses_WithCategory()
    {
        // M2: VerifiedFstAnalyzer must yield the matched HC analysis (real category), not the
        // category-less FST candidate. WordAnalysis.Equals includes Category, so set-equality vs the
        // engine fails if the category is dropped.
        var search = new Morpher(TraceManager, Language);
        IMorphologicalAnalyzer verified = new VerifiedFstAnalyzer(TraceManager, Language);
        foreach (string word in new[] { "sag", "dat" })
        {
            var fromSearch = new HashSet<WordAnalysis>(search.AnalyzeWord(word));
            List<WordAnalysis> fromVerified = verified.AnalyzeWord(word).ToList();
            Assert.That(fromVerified, Is.Not.Empty, $"expected analyses for {word}");
            foreach (WordAnalysis a in fromVerified)
            {
                Assert.That(a.Category, Is.Not.Null, $"verified analysis of {word} lost its category");
                Assert.That(
                    fromSearch,
                    Does.Contain(a),
                    $"verified analysis of {word} is not a genuine engine analysis"
                );
            }
        }
    }

    [Test]
    public void CompleteHybrid_PerWordOptOut_EngineMatchesSearch()
    {
        string[] corpus = { "sag", "dat" };
        var search = new Morpher(TraceManager, Language);
        var complete = CompleteHybridMorpher.FromLanguage(TraceManager, Language, corpus);
        foreach (string word in corpus)
        {
            var engine = new HashSet<string>(complete.AnalyzeWord(word, useFst: false).Select(Sig));
            var fst = new HashSet<string>(complete.AnalyzeWord(word, useFst: true).Select(Sig));
            var oracle = new HashSet<string>(search.AnalyzeWord(word).Select(Sig));
            Assert.That(engine.SetEquals(oracle), Is.True, $"engine opt-out path wrong for {word}");
            Assert.That(fst.SetEquals(oracle), Is.True, $"fst path wrong for {word}");
        }
    }

    [Test]
    public void Verified_ParallelMatchesSequential()
    {
        AddSuffix();
        IMorphologicalAnalyzer verified = new VerifiedFstAnalyzer(TraceManager, Language);
        var corpus = new List<string>();
        for (int i = 0; i < 50; i++)
        {
            corpus.AddRange(new[] { "sag", "sags", "dat", "sat", "saz", "sas", "sagg" });
        }
        Dictionary<string, string> sequential = corpus.Distinct().ToDictionary(w => w, w => SigSet(verified, w));
        var parallel = new ConcurrentDictionary<string, string>();
        Parallel.ForEach(corpus, w => parallel[w] = SigSet(verified, w));
        Assert.That(
            corpus.Distinct().All(w => parallel[w] == sequential[w]),
            Is.True,
            "concurrent analyses diverged from sequential"
        );
    }

    private static string Sig(WordAnalysis a) =>
        string.Join("+", a.Morphemes.Select(m => (m as Morpheme)?.Gloss ?? "?")) + ":" + a.RootMorphemeIndex;

    private static string SigSet(IMorphologicalAnalyzer analyzer, string word) =>
        string.Join("|", analyzer.AnalyzeWord(word).Select(Sig).OrderBy(s => s, System.StringComparer.Ordinal));
}
