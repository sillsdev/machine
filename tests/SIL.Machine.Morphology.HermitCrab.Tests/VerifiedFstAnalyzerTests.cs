using System.Collections.Concurrent;
using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;
using SIL.Machine.Morphology.HermitCrab.PhonologicalRules;

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

    [Test]
    public void Verified_CoversPhonologicallyAlteredBareRoot()
    {
        // Surface-allomorph precompile (§C): an unconditional t→d rule means the underlying bare root
        // "dat" (entry 8) can ONLY surface as "dad". The old proposer (underlying arcs) misses it — its
        // "t" arc can't match surface "d", and BareRootValid rejected it (it doesn't surface as itself).
        // The surface-precompile builds an arc from the actual generated surface ("dad"), so the altered
        // bare root is now matched. Confirmed via probe: gen dat(8)→dad, and "dad" analyzes while "dat"
        // no longer does.
        var tToD = new RewriteRule
        {
            Name = "t_to_d",
            Lhs = Pattern<Word, ShapeNode>.New().Annotation(Character(Table1, "t")).Value,
        };
        tToD.Subrules.Add(
            new RewriteSubrule { Rhs = Pattern<Word, ShapeNode>.New().Annotation(Character(Table1, "d")).Value }
        );
        Surface.PhonologicalRules.Add(tToD);
        try
        {
            var search = new Morpher(TraceManager, Language);
            Assert.That(
                search.AnalyzeWord("dad").Any(),
                Is.True,
                "precondition: 'dad' analyzes (bare root 'dat' surfaces as 'dad')"
            );

            // Baseline: the underlying-only proposer (no-morpher ctor builds arcs from underlying shapes)
            // misses the altered surface — both "dad" readings are underlying "dat", so it has no "dad" arc.
            Assert.That(
                new FstTemplateAnalyzer(Language).AnalyzeWord("dad"),
                Is.Empty,
                "baseline: the underlying-only proposer must miss the phonologically-altered surface"
            );

            IMorphologicalAnalyzer verified = new VerifiedFstAnalyzer(TraceManager, Language);
            AnalysisComparison cmp = FstVerification.Compare(search, verified, new[] { "dad" });
            Assert.That(cmp.IsComplete, Is.True, "altered bare root not covered: " + cmp.Format());

            Assert.That(verified.AnalyzeWord("zzz"), Is.Empty, "soundness: a non-word must still yield nothing");
        }
        finally
        {
            Surface.PhonologicalRules.Remove(tToD);
        }
    }

    [Test]
    public void Composite_CoversFullReduplication_WhereFstAloneMisses()
    {
        // Point 3: full reduplication (copy the whole stem) is non-regular — the FST cannot represent
        // it, but the ReduplicationProposer strips one copy, recurses the residual through the FST, and
        // wraps it with the reduplication morpheme; verify confirms it as a genuine HC analysis.
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        var redup = new AffixProcessRule
        {
            Name = "redup",
            Gloss = "RED",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
            OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
        };
        redup.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new CopyFromInput("1") }, // copy the stem twice
            }
        );
        Morphophonemic.MorphologicalRules.Add(redup);
        try
        {
            var search = new Morpher(TraceManager, Language);
            Assert.That(search.AnalyzeWord("sagsag").Any(), Is.True, "precondition: 'sagsag' = RED('sag')");

            var fst = new FstTemplateAnalyzer(Language, new Morpher(TraceManager, Language));
            Assert.That(fst.AnalyzeWord("sagsag"), Is.Empty, "baseline: the FST alone cannot represent reduplication");
            Assert.That(fst.CoversAllConstructs, Is.False, "reduplication marks the FST not-fully-covered");

            var composite = new CompositeProposer(fst, new ReduplicationProposer(Language, fst));
            Assert.That(composite.CoversAllConstructs, Is.True, "the reduplication generator covers the skipped op");

            var pool = new MorpherPool(() => new Morpher(new TraceManager(), Language));
            IMorphologicalAnalyzer verified = new VerifiedFstAnalyzer(composite, pool);
            AnalysisComparison cmp = FstVerification.Compare(search, verified, new[] { "sagsag" });
            Assert.That(cmp.IsComplete, Is.True, "reduplication not covered: " + cmp.Format());

            Assert.That(verified.AnalyzeWord("zzz"), Is.Empty, "soundness: a non-word must still yield nothing");
        }
        finally
        {
            Morphophonemic.MorphologicalRules.Remove(redup);
        }
    }

    private static string Sig(WordAnalysis a) =>
        string.Join("+", a.Morphemes.Select(m => (m as Morpheme)?.Gloss ?? "?")) + ":" + a.RootMorphemeIndex;

    private static string SigSet(IMorphologicalAnalyzer analyzer, string word) =>
        string.Join("|", analyzer.AnalyzeWord(word).Select(Sig).OrderBy(s => s, System.StringComparer.Ordinal));
}
